using colosseumbackend.Common;
using colosseumbackend.Configuration;
using colosseumbackend.Data.Entities;
using colosseumbackend.Dtos;
using colosseumbackend.Logging;
using colosseumbackend.Mappers;
using colosseumbackend.Repositories;
using colosseumbackend.Solana;
using Microsoft.Extensions.Options;
using Solnet.Wallet;

namespace colosseumbackend.Services;

public class ListingService : BaseService
{
    private readonly ListingRepository _listingRepository;
    private readonly GameRepository    _gameRepository;
    private readonly SolanaGateway     _solana;
    private readonly SolanaOptions     _solanaOptions;

    public ListingService(
        ListingRepository       listingRepository,
        GameRepository          gameRepository,
        SolanaGateway           solana,
        IOptions<SolanaOptions> solanaOptions,
        DatabaseLogger          dbLogger
        ) : base(dbLogger)
    {
        _listingRepository = listingRepository;
        _gameRepository    = gameRepository;
        _solana            = solana;
        _solanaOptions     = solanaOptions.Value;
    }

    public async Task<ListResponse<MinimalListingDto>> SearchAsync(ListingSearchQuery query)
    {
        var response = new ListResponse<MinimalListingDto>();
        var game     = string.IsNullOrWhiteSpace(query.Game) ? null : await _gameRepository.GetBySlugAsync(query.Game);
        bool unknownGame = !string.IsNullOrWhiteSpace(query.Game) && game is null;

        var listings = unknownGame
            ? []
            : await _listingRepository.SearchActiveAsync(game?.Id, query.Type, query.Q, query.PageNumber, query.PageSize);

        response.Ok(listings.Select(ListingMapper.ToMinimal).ToList(), ResponseMessages.ListingsRetrieved);
        return response;
    }

    /// <summary>The caller's own listings, paused ones included (the seller dashboard).</summary>
    public async Task<ListResponse<FullListingDto>> GetMineAsync(string sellerWallet, PageQuery pageQuery)
    {
        var response = new ListResponse<FullListingDto>();
        var listings = await _listingRepository.GetBySellerAsync(sellerWallet, pageQuery.PageNumber, pageQuery.PageSize);

        response.Ok(listings.Select(ListingMapper.ToFull).ToList(), ResponseMessages.ListingsRetrieved);
        return response;
    }

    /// <summary>
    /// The shareable deal link. Includes the seller's on-chain record and, for a signed-in buyer,
    /// the deposits the program would set if they bought now.
    /// </summary>
    public async Task<ApplicationResponse<StandardListingDto>> GetByIdAsync(Guid id, string? callerWallet)
    {
        var response = new ApplicationResponse<StandardListingDto>();
        var listing  = await _listingRepository.GetByIdAsync(id);

        if (listing is not null)
        {
            var sellerRecord = (await _solana.GetReputationAsync(new PublicKey(listing.SellerWallet))).AsSeller;
            bool isOtherBuyer = callerWallet is not null && callerWallet != listing.SellerWallet;
            var preview      = isOtherBuyer ? await PreviewDepositsAsync(listing, callerWallet!, sellerRecord) : null;

            response.Ok(ListingMapper.ToStandard(listing, sellerRecord, preview), ResponseMessages.ListingRetrieved);
        }
        else
        {
            response.Fail(ResponseMessages.ListingNotFound);
        }

        return response;
    }

    public async Task<ApplicationResponse<StandardListingDto>> CreateAsync(string sellerWallet, CreateListingDto createListingDto)
    {
        var response   = new ApplicationResponse<StandardListingDto>();
        var validation = await ValidateTermsAsync(createListingDto);

        if (validation.Succeeded)
        {
            var listingToAdd = ListingMapper.ToEntity(sellerWallet, createListingDto, validation.Data.Price, validation.Data.DeliveryWindow);
            await _listingRepository.AddAsync(listingToAdd);

            var listing      = (await _listingRepository.GetByIdAsync(listingToAdd.Id))!;
            var sellerRecord = (await _solana.GetReputationAsync(new PublicKey(sellerWallet))).AsSeller;
            response.Ok(ListingMapper.ToStandard(listing, sellerRecord, null), ResponseMessages.ListingCreated);
            await LogResultAsync(response.Succeeded, nameof(CreateAsync), nameof(Listing), null, listing.Id.ToString(), sellerWallet);
        }
        else
        {
            response.Fail(validation.Message);
            await LogResultAsync(response.Succeeded, nameof(CreateAsync), nameof(Listing), response.Message, null, sellerWallet);
        }

        return response;
    }

    /// <summary>Edits apply to future deals only; existing deals keep the terms they were created with.</summary>
    public async Task<ApplicationResponse<StandardListingDto>> UpdateAsync(Guid id, string sellerWallet, EditListingDto editListingDto)
    {
        var response = new ApplicationResponse<StandardListingDto>();
        var listing  = await _listingRepository.GetOwnedAsync(id, sellerWallet);

        if (listing is not null)
        {
            var validation = await ValidateTermsAsync(editListingDto);
            if (validation.Succeeded)
            {
                listing.GameId                = editListingDto.GameId;
                listing.Type                  = editListingDto.Type;
                listing.Title                 = editListingDto.Title.Trim();
                listing.Description           = editListingDto.Description.Trim();
                listing.Price                 = validation.Data.Price;
                listing.Milestones            = editListingDto.Milestones;
                listing.Unit                  = editListingDto.Unit;
                listing.DeliveryWindowSeconds = (long)validation.Data.DeliveryWindow.TotalSeconds;
                await _listingRepository.SaveChangesAsync();

                var reloaded     = (await _listingRepository.GetByIdAsync(id))!;
                var sellerRecord = (await _solana.GetReputationAsync(new PublicKey(sellerWallet))).AsSeller;
                response.Ok(ListingMapper.ToStandard(reloaded, sellerRecord, null), ResponseMessages.ListingUpdated);
            }
            else
            {
                response.Fail(validation.Message);
            }
        }
        else
        {
            response.Fail(ResponseMessages.ListingNotFound);
        }

        await LogResultAsync(response.Succeeded, nameof(UpdateAsync), nameof(Listing), response.Succeeded ? null : response.Message, id.ToString(), sellerWallet);
        return response;
    }

    public async Task<ApplicationResponse> SetActiveAsync(Guid id, string sellerWallet, bool isActive)
    {
        var response = new ApplicationResponse();
        var listing  = await _listingRepository.GetOwnedAsync(id, sellerWallet);

        if (listing is not null)
        {
            listing.IsActive = isActive;
            await _listingRepository.SaveChangesAsync();
            response.Ok(ResponseMessages.ListingUpdated);
        }
        else
        {
            response.Fail(ResponseMessages.ListingNotFound);
        }

        await LogResultAsync(response.Succeeded, nameof(SetActiveAsync), nameof(Listing), $"IsActive={isActive}", id.ToString(), sellerWallet);
        return response;
    }

    private async Task<DepositPreviewDto?> PreviewDepositsAsync(Listing listing, string buyerWallet, RoleRecord sellerRecord)
    {
        DepositPreviewDto? preview = null;
        var config = await _solana.GetConfigAsync();

        if (config is not null)
        {
            var   buyerRecord = (await _solana.GetReputationAsync(new PublicKey(buyerWallet))).AsBuyer;
            ulong price       = (ulong)listing.Price;
            preview = new DepositPreviewDto
            {
                BuyerDeposit  = AmountMapper.ToDto(config.Deposit.Amount(buyerRecord, price)),
                SellerDeposit = AmountMapper.ToDto(config.Deposit.Amount(sellerRecord, price)),
            };
        }

        return preview;
    }

    private async Task<ApplicationResponse<(long Price, TimeSpan DeliveryWindow)>> ValidateTermsAsync(CreateListingDto terms)
    {
        var response = new ApplicationResponse<(long Price, TimeSpan DeliveryWindow)>();

        bool gameExists       = await _gameRepository.ExistsAsync(terms.GameId);
        bool knownWindow      = DeliveryWindows.TryGet(_solanaOptions.Cluster, terms.DeliveryWindow, out var deliveryWindow);
        bool parsedPrice      = long.TryParse(terms.Price, out long price) && price > 0;
        bool divisibleByParts = parsedPrice && price % terms.Milestones == 0;

        if (!gameExists)
        {
            response.Fail(ResponseMessages.GameNotFound);
        }
        else if (!knownWindow)
        {
            response.Fail(ResponseMessages.InvalidDeliveryWindow);
        }
        else if (!divisibleByParts)
        {
            response.Fail(ResponseMessages.PriceNotDivisible);
        }
        else
        {
            response.Ok((price, deliveryWindow));
        }

        return response;
    }
}
