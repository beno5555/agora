using colosseumbackend.Common;
using colosseumbackend.Data.Entities;
using colosseumbackend.Dtos;
using colosseumbackend.Logging;
using colosseumbackend.Mappers;
using colosseumbackend.Repositories;
using colosseumbackend.Solana;
using Solnet.Wallet;

namespace colosseumbackend.Services;

public class UserService : BaseService
{
    private readonly UserRepository    _userRepository;
    private readonly ListingRepository _listingRepository;
    private readonly SolanaGateway     _solana;

    public UserService(UserRepository userRepository, ListingRepository listingRepository, SolanaGateway solana, DatabaseLogger dbLogger) : base(dbLogger)
    {
        _userRepository    = userRepository;
        _listingRepository = listingRepository;
        _solana            = solana;
    }

    public async Task<ApplicationResponse<FullUserDto>> GetMeAsync(string wallet)
    {
        var response = new ApplicationResponse<FullUserDto>();
        var user     = await _userRepository.GetByWalletAsync(wallet);

        if (user is not null)
        {
            response.Ok(UserMapper.ToFull(user), ResponseMessages.UserRetrieved);
        }
        else
        {
            response.Fail(ResponseMessages.UserNotFound);
        }

        return response;
    }

    public async Task<ApplicationResponse<FullUserDto>> UpdateMeAsync(string wallet, EditUserDto editUserDto)
    {
        var response = new ApplicationResponse<FullUserDto>();
        var user     = await _userRepository.GetByWalletAsync(wallet);

        if (user is not null)
        {
            user.DisplayName = string.IsNullOrWhiteSpace(editUserDto.DisplayName) ? null : editUserDto.DisplayName.Trim();
            user.Discord     = string.IsNullOrWhiteSpace(editUserDto.Discord) ? null : editUserDto.Discord.Trim();
            await _userRepository.SaveChangesAsync();

            response.Ok(UserMapper.ToFull(user), ResponseMessages.UserUpdated);
            await LogResultAsync(response.Succeeded, nameof(UpdateMeAsync), nameof(User), null, wallet, wallet);
        }
        else
        {
            response.Fail(ResponseMessages.UserNotFound);
            await LogResultAsync(response.Succeeded, nameof(UpdateMeAsync), nameof(User), response.Message, wallet, wallet);
        }

        return response;
    }

    /// <summary>Public profile: reputation is read from the chain, so nobody can fake it.</summary>
    public async Task<ApplicationResponse<StandardUserDto>> GetProfileAsync(string wallet)
    {
        var response = new ApplicationResponse<StandardUserDto>();
        var user     = WalletSignIn.IsValidWallet(wallet) ? await _userRepository.GetByWalletAsync(wallet) : null;

        if (user is not null)
        {
            var record   = await _solana.GetReputationAsync(new PublicKey(wallet));
            var listings = await _listingRepository.GetActiveBySellerAsync(wallet);
            response.Ok(UserMapper.ToStandard(user, record, listings), ResponseMessages.UserRetrieved);
        }
        else
        {
            response.Fail(ResponseMessages.UserNotFound);
        }

        return response;
    }
}

public class GameService
{
    private readonly GameRepository _gameRepository;

    public GameService(GameRepository gameRepository)
    {
        _gameRepository = gameRepository;
    }

    public async Task<ListResponse<GameDto>> GetAllAsync()
    {
        var response = new ListResponse<GameDto>();
        var games    = await _gameRepository.GetAllWithListingCountsAsync();

        response.Ok(games.Select(row => GameMapper.ToDto(row.Game, row.ActiveListings)).ToList(), ResponseMessages.GamesRetrieved);
        return response;
    }
}

public class ListingService : BaseService
{
    private readonly ListingRepository _listingRepository;
    private readonly GameRepository    _gameRepository;
    private readonly SolanaGateway     _solana;

    public ListingService(ListingRepository listingRepository, GameRepository gameRepository, SolanaGateway solana, DatabaseLogger dbLogger) : base(dbLogger)
    {
        _listingRepository = listingRepository;
        _gameRepository    = gameRepository;
        _solana            = solana;
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
            var sellerRecord = await _solana.GetReputationAsync(new PublicKey(listing.SellerWallet));
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
            var sellerRecord = await _solana.GetReputationAsync(new PublicKey(sellerWallet));
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
                listing.UnitLabel             = editListingDto.UnitLabel.Trim();
                listing.DeliveryWindowSeconds = (long)validation.Data.DeliveryWindow.TotalSeconds;
                await _listingRepository.SaveChangesAsync();

                var reloaded     = (await _listingRepository.GetByIdAsync(id))!;
                var sellerRecord = await _solana.GetReputationAsync(new PublicKey(sellerWallet));
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

    private async Task<DepositPreviewDto?> PreviewDepositsAsync(Listing listing, string buyerWallet, ReputationAccount sellerRecord)
    {
        DepositPreviewDto? preview = null;
        var config = await _solana.GetConfigAsync();

        if (config is not null)
        {
            var   buyerRecord = await _solana.GetReputationAsync(new PublicKey(buyerWallet));
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
        bool knownWindow      = Constants.DeliveryWindowPresets.TryGetValue(terms.DeliveryWindow, out var deliveryWindow);
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

public class NotificationService
{
    private readonly NotificationRepository _notificationRepository;

    public NotificationService(NotificationRepository notificationRepository)
    {
        _notificationRepository = notificationRepository;
    }

    public async Task<ListResponse<NotificationDto>> GetMineAsync(string wallet, PageQuery pageQuery)
    {
        var response      = new ListResponse<NotificationDto>();
        var notifications = await _notificationRepository.GetForWalletAsync(wallet, pageQuery.PageNumber, pageQuery.PageSize);

        response.Ok(notifications.Select(NotificationMapper.ToDto).ToList(), ResponseMessages.NotificationsRetrieved);
        return response;
    }

    public async Task<ApplicationResponse> MarkReadAsync(long id, string wallet)
    {
        var  response = new ApplicationResponse();
        bool updated  = await _notificationRepository.MarkReadAsync(id, wallet);

        if (updated)
        {
            response.Ok(ResponseMessages.NotificationRead);
        }
        else
        {
            response.Fail(ResponseMessages.NotificationNotFound);
        }

        return response;
    }
}
