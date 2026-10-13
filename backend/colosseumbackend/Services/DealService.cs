using System.Security.Cryptography;
using colosseumbackend.Common;
using colosseumbackend.Data.Entities;
using colosseumbackend.Dtos;
using colosseumbackend.Logging;
using colosseumbackend.Mappers;
using colosseumbackend.Repositories;
using colosseumbackend.Solana;
using Solnet.Rpc.Models;
using Solnet.Wallet;

namespace colosseumbackend.Services;

/// <summary>
/// Deal lifecycle off-chain: creating drafts, building the unsigned transaction for each
/// action, and syncing after the user's wallet has sent it. Never signs or moves funds.
/// </summary>
public class DealService : BaseService
{
    private readonly DealRepository    _dealRepository;
    private readonly ListingRepository _listingRepository;
    private readonly DealSyncService   _dealSyncService;
    private readonly SolanaGateway     _solana;

    public DealService(
        DealRepository    dealRepository,
        ListingRepository listingRepository,
        DealSyncService   dealSyncService,
        SolanaGateway     solana,
        DatabaseLogger    dbLogger
        ) : base(dbLogger)
    {
        _dealRepository    = dealRepository;
        _listingRepository = listingRepository;
        _dealSyncService   = dealSyncService;
        _solana            = solana;
    }

    /// <summary>Creates a draft deal and returns the buyer's unsigned create_deal transaction.</summary>
    public async Task<ApplicationResponse<DealTransactionDto>> CreateAsync(string buyerWallet, CreateDealDto createDealDto)
    {
        var response = new ApplicationResponse<DealTransactionDto>();
        var listing  = await _listingRepository.GetByIdAsync(createDealDto.ListingId);

        bool isAvailable = listing is not null && listing.IsActive;
        bool isOwnListing = listing is not null && listing.SellerWallet == buyerWallet;

        if (isAvailable && !isOwnListing)
        {
            var   buyer  = new PublicKey(buyerWallet);
            ulong dealId = BitConverter.ToUInt64(RandomNumberGenerator.GetBytes(sizeof(ulong)));
            var   pda    = _solana.Program.DealAddress(buyer, dealId);
            var   deal   = DealMapper.ToDraft(listing!, buyerWallet, dealId, pda.Key, createDealDto.BuyerNote);
            await _dealRepository.AddAsync(deal);

            var instruction = _solana.Program.CreateDeal(buyer, dealId, new PublicKey(listing!.SellerWallet),
                (ulong)listing.Price, (ushort)listing.Milestones, listing.DeliveryWindowSeconds);
            string transaction = await _solana.BuildUnsignedTransactionAsync(buyer, [instruction]);

            deal.Listing = listing;
            response.Ok(new DealTransactionDto
            {
                Deal        = DealMapper.ToFull(deal, buyerWallet, []),
                Transaction = transaction,
            }, ResponseMessages.DealCreated);
            await LogResultAsync(response.Succeeded, nameof(CreateAsync), nameof(Deal), null, deal.Id.ToString(), buyerWallet);
        }
        else
        {
            response.Fail(isOwnListing ? ResponseMessages.CannotBuyOwnListing : ResponseMessages.ListingNotFound);
            await LogResultAsync(response.Succeeded, nameof(CreateAsync), nameof(Deal), response.Message, createDealDto.ListingId.ToString(), buyerWallet);
        }

        return response;
    }

    public async Task<ApplicationResponse<FullDealDto>> GetAsync(Guid id, string wallet)
    {
        var response = new ApplicationResponse<FullDealDto>();
        var deal     = await _dealRepository.GetForPartyAsync(id, wallet);

        if (deal is not null)
        {
            response.Ok(DealMapper.ToFull(deal, wallet, DealRules.AllowedActions(deal, wallet, DateTime.UtcNow)), ResponseMessages.DealRetrieved);
        }
        else
        {
            response.Fail(ResponseMessages.DealNotFound);
        }

        return response;
    }

    public async Task<ListResponse<MinimalDealDto>> GetMineAsync(string wallet, DealQuery dealQuery)
    {
        var  response = new ListResponse<MinimalDealDto>();
        bool? asBuyer = dealQuery.Role switch
        {
            DealMapper.BuyerRole  => true,
            DealMapper.SellerRole => false,
            _                     => null,
        };

        var deals = await _dealRepository.GetForUserAsync(wallet, asBuyer, dealQuery.Status, dealQuery.PageNumber, dealQuery.PageSize);
        var now   = DateTime.UtcNow;
        response.Ok(deals.Select(deal => DealMapper.ToMinimal(deal, wallet, DealRules.AllowedActions(deal, wallet, now))).ToList(), ResponseMessages.DealsRetrieved);
        return response;
    }

    /// <summary>Builds the unsigned transaction for one of the caller's currently allowed actions.</summary>
    public async Task<ApplicationResponse<DealTransactionDto>> BuildActionTransactionAsync(Guid id, string wallet, DealActionDto dealActionDto)
    {
        var response = new ApplicationResponse<DealTransactionDto>();
        var deal     = await _dealRepository.GetForPartyAsync(id, wallet);

        if (deal is not null)
        {
            var  allowedActions = DealRules.AllowedActions(deal, wallet, DateTime.UtcNow);
            bool isAllowed      = allowedActions.Contains(dealActionDto.Action);

            if (isAllowed)
            {
                var instructions = await BuildInstructionsAsync(deal, wallet, dealActionDto);
                if (instructions.Succeeded)
                {
                    string transaction = await _solana.BuildUnsignedTransactionAsync(new PublicKey(wallet), instructions.Data!);
                    response.Ok(new DealTransactionDto
                    {
                        Deal        = DealMapper.ToFull(deal, wallet, allowedActions),
                        Transaction = transaction,
                    }, ResponseMessages.TransactionBuilt);
                }
                else
                {
                    response.Fail(instructions.Message);
                }
            }
            else
            {
                response.Fail(ResponseMessages.ActionNotAllowed);
            }
        }
        else
        {
            response.Fail(ResponseMessages.DealNotFound);
        }

        if (!response.Succeeded)
        {
            await LogResultAsync(response.Succeeded, nameof(BuildActionTransactionAsync), nameof(Deal), $"{dealActionDto.Action}: {response.Message}", id.ToString(), wallet);
        }

        return response;
    }

    /// <summary>
    /// Called after the caller's wallet sent a transaction: waits for it to confirm, then copies
    /// the deal's on-chain state (or final outcome) into the database.
    /// </summary>
    public async Task<ApplicationResponse<FullDealDto>> SyncAsync(Guid id, string wallet, SyncDealDto syncDealDto)
    {
        var response = new ApplicationResponse<FullDealDto>();
        var deal     = await _dealRepository.GetForPartyAsync(id, wallet);

        if (deal is not null)
        {
            bool isConfirmed = await _solana.WaitForConfirmationAsync(syncDealDto.Signature);
            if (isConfirmed)
            {
                await _dealSyncService.SyncAsync(deal, syncDealDto.Signature);
                response.Ok(DealMapper.ToFull(deal, wallet, DealRules.AllowedActions(deal, wallet, DateTime.UtcNow)), ResponseMessages.DealSynced);
            }
            else
            {
                response.Fail(ResponseMessages.TransactionNotFound);
            }
        }
        else
        {
            response.Fail(ResponseMessages.DealNotFound);
        }

        await LogResultAsync(response.Succeeded, nameof(SyncAsync), nameof(Deal), response.Succeeded ? null : response.Message, id.ToString(), wallet);
        return response;
    }

    private async Task<ApplicationResponse<List<TransactionInstruction>>> BuildInstructionsAsync(Deal deal, string wallet, DealActionDto dealActionDto)
    {
        var response = new ApplicationResponse<List<TransactionInstruction>>();
        var program  = _solana.Program;
        var actor    = new PublicKey(wallet);
        var dealKey  = new PublicKey(deal.DealPda);
        long remaining = DealRules.RemainingPrice(deal);

        switch (dealActionDto.Action)
        {
            case DealAction.Accept:
                response.Ok([program.AcceptDeal(actor, dealKey)]);
                break;

            case DealAction.MarkDelivered:
                response.Ok([program.DealAction("mark_delivered", actor, dealKey)]);
                break;

            case DealAction.RaiseDispute:
                response.Ok([program.DealAction("raise_dispute", actor, dealKey)]);
                break;

            case DealAction.ProposeSplit:
                bool validAmount = long.TryParse(dealActionDto.BuyerAmount, out long buyerAmount) && buyerAmount <= remaining;
                if (validAmount)
                {
                    response.Ok([program.DealAction("propose_split", actor, dealKey, new BorshWriter().U64((ulong)buyerAmount).ToArray())]);
                }
                else
                {
                    response.Fail(ResponseMessages.InvalidActionInput);
                }
                break;

            case DealAction.Confirm:
                int  parts      = dealActionDto.Parts ?? 0;
                bool validParts = parts >= 1 && deal.MilestonesReleased + parts <= deal.Milestones;
                if (validParts)
                {
                    response = await SettleInstructionsAsync(deal, actor, "confirm", new BorshWriter().U16((ushort)parts).ToArray());
                }
                else
                {
                    response.Fail(ResponseMessages.InvalidActionInput);
                }
                break;

            case DealAction.RefundUndelivered:
                response = await SettleInstructionsAsync(deal, actor, "refund_undelivered", null);
                break;

            case DealAction.AcceptSplit:
                response = await SettleInstructionsAsync(deal, actor, "accept_split", null);
                break;

            case DealAction.Cancel:
                response = await SettleInstructionsAsync(deal, actor, "cancel_deal", null);
                break;

            case DealAction.SellerRefund:
                response = await SettleInstructionsAsync(deal, actor, "seller_refund", null);
                break;
        }

        return response;
    }

    /// <summary>
    /// Payout instructions need both parties' token accounts and the treasury. Missing token
    /// accounts are created first (no-op if they exist), paid by the caller.
    /// </summary>
    private async Task<ApplicationResponse<List<TransactionInstruction>>> SettleInstructionsAsync(Deal deal, PublicKey actor, string instructionName, byte[]? args)
    {
        var response = new ApplicationResponse<List<TransactionInstruction>>();
        var config   = await _solana.GetConfigAsync();

        if (config is not null)
        {
            var program = _solana.Program;
            var buyer   = new PublicKey(deal.BuyerWallet);
            var seller  = new PublicKey(deal.SellerWallet);
            response.Ok(
            [
                program.CreateTokenAccountIfMissing(actor, buyer),
                program.CreateTokenAccountIfMissing(actor, seller),
                program.Settle(instructionName, actor, new PublicKey(deal.DealPda), buyer, seller, config.Treasury, args),
            ]);
        }
        else
        {
            response.Fail(ResponseMessages.ChainUnavailable);
        }

        return response;
    }
}
