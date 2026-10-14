using colosseumbackend.Common;
using colosseumbackend.Data.Entities;
using colosseumbackend.Logging;
using colosseumbackend.Repositories;
using colosseumbackend.Solana;
using Solnet.Wallet;

namespace colosseumbackend.Services;

/// <summary>
/// Copies on-chain deal state into the database and creates notifications for every change.
/// Used after each user transaction (sync endpoint) and by the background job.
/// </summary>
public class DealSyncService : BaseService
{
    // How many recent transactions to scan for the DealClosed event when no signature is known.
    private const ulong ClosedDealSignatureScan = 10;

    private readonly DealRepository         _dealRepository;
    private readonly NotificationRepository _notificationRepository;
    private readonly SolanaGateway          _solana;
    private readonly ILogger<DealSyncService> _logger;

    public DealSyncService(
        DealRepository         dealRepository,
        NotificationRepository notificationRepository,
        SolanaGateway          solana,
        DatabaseLogger         dbLogger,
        ILogger<DealSyncService> logger
        ) : base(dbLogger)
    {
        _dealRepository         = dealRepository;
        _notificationRepository = notificationRepository;
        _solana                 = solana;
        _logger                 = logger;
    }

    /// <summary>Re-reads one deal from the chain. `signature` (if known) is the transaction that just touched it.</summary>
    public async Task<bool> SyncAsync(Deal deal, string? signature)
    {
        var result = await _solana.GetDealAsync(new PublicKey(deal.DealPda));
        if (result.IsCompatible)
        {
            await ApplyAsync(deal, result.Account, signature);
        }

        return result.IsCompatible;
    }

    /// <summary>Background pass: syncs up to `batchSize` open deals with one RPC call, then sends deadline reminders.</summary>
    public async Task SyncOpenDealsAsync(int batchSize)
    {
        var deals = await _dealRepository.GetOpenAsync(batchSize);
        if (deals.Count > 0)
        {
            var accounts = await _solana.GetDealsAsync(deals.Select(deal => deal.DealPda).ToList());
            var compatibleDeals = new List<Deal>();
            for (int index = 0; index < deals.Count; index++)
            {
                var result = accounts[index];
                if (result.IsCompatible)
                {
                    await ApplyAsync(deals[index], result.Account, null);
                    compatibleDeals.Add(deals[index]);
                }
                else
                {
                    // Rotate incompatible accounts out of the oldest-synced batch without inventing
                    // a close/refund outcome or letting them block unrelated deals and draft expiry.
                    deals[index].SyncedAt = DateTime.UtcNow;
                }
            }

            await _dealRepository.SaveChangesAsync();
            await SendDeadlineNoticesAsync(compatibleDeals);
            int incompatibleCount = deals.Count - compatibleDeals.Count;
            if (incompatibleCount > 0)
            {
                _logger.LogWarning("Skipped {Count} incompatible on-chain deal accounts; other deals were synced", incompatibleCount);
            }
        }
    }

    private async Task ApplyAsync(Deal deal, DealAccount? account, string? signature)
    {
        var  previous      = Snapshot(deal);
        bool accountExists = account is not null;
        bool wasOpenOnChain = deal.Status is DealStatus.AwaitingSeller or DealStatus.Active or DealStatus.Delivered or DealStatus.Disputed;

        if (accountExists)
        {
            CopyFromChain(deal, account!);
        }
        else if (wasOpenOnChain)
        {
            var outcome = await FindClosedOutcomeAsync(deal, signature);
            if (outcome is not null)
            {
                deal.Status = ToFinalStatus(outcome.Value);

                // The closed account can't be read any more; a completed deal released every part.
                bool releasedEverything = deal.Status == DealStatus.Completed;
                if (releasedEverything)
                {
                    deal.MilestonesReleased = deal.Milestones;
                }
            }
        }
        else if (deal.Status == DealStatus.Draft && deal.CreatedAt + Constants.DraftLifetime < DateTime.UtcNow)
        {
            deal.Status = DealStatus.Expired;
        }

        deal.LastSignature = signature ?? deal.LastSignature;
        deal.SyncedAt      = DateTime.UtcNow;
        await _dealRepository.SaveChangesAsync();

        var notifications = NotificationsForChange(previous, deal);
        if (notifications.Count > 0)
        {
            await _notificationRepository.AddRangeAsync(notifications);
            await LogResultAsync(true, nameof(SyncAsync), nameof(Deal), $"{previous.Status} -> {deal.Status}", deal.Id.ToString(), null);
        }
    }

    private static void CopyFromChain(Deal deal, DealAccount account)
    {
        deal.Status                = ToOpenStatus(account.Status);
        deal.Price                 = (long)account.Price;
        deal.Milestones            = account.Milestones;
        deal.MilestonesReleased    = account.MilestonesReleased;
        deal.BuyerDeposit          = (long)account.BuyerDeposit;
        deal.SellerDeposit         = (long)account.SellerDeposit;
        deal.DeliveryWindowSeconds = account.DeliveryWindow;
        deal.ReviewWindowSeconds   = account.ReviewWindow;
        deal.AcceptedAt            = FromUnix(account.AcceptedAt);
        deal.DeliveredAt           = FromUnix(account.DeliveredAt);
        deal.SplitOfferBy          = account.SplitOffer?.By.Key;
        deal.SplitOfferBuyerAmount = account.SplitOffer is null ? null : (long)account.SplitOffer.BuyerAmount;
    }

    private async Task<OnchainOutcome?> FindClosedOutcomeAsync(Deal deal, string? signature)
    {
        var dealAddress = new PublicKey(deal.DealPda);
        List<string> candidates = signature is not null
            ? [signature]
            : await _solana.GetRecentSignaturesAsync(dealAddress, ClosedDealSignatureScan);

        OnchainOutcome? outcome = null;
        foreach (string candidate in candidates)
        {
            outcome ??= await _solana.GetDealClosedOutcomeAsync(candidate, dealAddress);
        }

        return outcome;
    }

    private async Task SendDeadlineNoticesAsync(List<Deal> deals)
    {
        var  now           = DateTime.UtcNow;
        var  notifications = new List<Notification>();

        foreach (var deal in deals.Where(deal => deal.Status == DealStatus.Active && deal.DeliveryDeadline is not null))
        {
            bool deadlineSoon   = now >= deal.DeliveryDeadline!.Value - Constants.DeadlineReminderLead && now <= deal.DeliveryDeadline;
            bool deadlinePassed = now > deal.DeliveryDeadline;

            if (deadlineSoon && !deal.DeadlineReminderSent)
            {
                notifications.Add(NewNotification(deal.SellerWallet, deal, NotificationKind.DeadlineSoon));
                deal.DeadlineReminderSent = true;
            }

            if (deadlinePassed && !deal.RefundNoticeSent)
            {
                notifications.Add(NewNotification(deal.BuyerWallet, deal, NotificationKind.RefundAvailable));
                deal.RefundNoticeSent = true;
            }
        }

        foreach (var deal in deals.Where(deal => deal.Status == DealStatus.Delivered && deal.ReviewDeadline is not null))
        {
            bool reviewEndingSoon = now >= deal.ReviewDeadline!.Value - Constants.ReviewReminderLead && now <= deal.ReviewDeadline;
            bool reviewOver       = now > deal.ReviewDeadline;

            if (reviewEndingSoon && !deal.ReviewReminderSent)
            {
                notifications.Add(NewNotification(deal.BuyerWallet, deal, NotificationKind.ReviewEndingSoon));
                deal.ReviewReminderSent = true;
            }

            if (reviewOver && !deal.ClaimNoticeSent)
            {
                notifications.Add(NewNotification(deal.SellerWallet, deal, NotificationKind.PaymentClaimable));
                deal.ClaimNoticeSent = true;
            }
        }

        if (notifications.Count > 0)
        {
            await _notificationRepository.AddRangeAsync(notifications); // also saves the flags above
        }
    }

    private static List<Notification> NotificationsForChange(DealSnapshot previous, Deal deal)
    {
        var  notifications  = new List<Notification>();
        bool statusChanged  = previous.Status != deal.Status;
        bool partsReleased  = deal.MilestonesReleased > previous.MilestonesReleased;
        bool newSplitOffer  = deal.SplitOfferBy is not null
            && (deal.SplitOfferBy != previous.SplitOfferBy || deal.SplitOfferBuyerAmount != previous.SplitOfferBuyerAmount);
        bool isFinal        = deal.Status is DealStatus.Completed or DealStatus.Split or DealStatus.Undelivered
                                          or DealStatus.SellerRefunded or DealStatus.Cancelled;

        if (statusChanged)
        {
            switch (deal.Status)
            {
                case DealStatus.AwaitingSeller:
                    notifications.Add(NewNotification(deal.SellerWallet, deal, NotificationKind.DealCreated));
                    break;
                case DealStatus.Active:
                    notifications.Add(NewNotification(deal.BuyerWallet, deal, NotificationKind.DealAccepted));
                    break;
                case DealStatus.Delivered:
                    notifications.Add(NewNotification(deal.BuyerWallet, deal, NotificationKind.DealDelivered));
                    break;
                case DealStatus.Disputed:
                    notifications.Add(NewNotification(deal.BuyerWallet, deal, NotificationKind.DealDisputed));
                    notifications.Add(NewNotification(deal.SellerWallet, deal, NotificationKind.DealDisputed));
                    break;
            }

            if (isFinal)
            {
                notifications.Add(NewNotification(deal.BuyerWallet, deal, NotificationKind.DealClosed));
                notifications.Add(NewNotification(deal.SellerWallet, deal, NotificationKind.DealClosed));
            }
        }

        if (partsReleased)
        {
            notifications.Add(NewNotification(deal.SellerWallet, deal, NotificationKind.PartsConfirmed));
        }

        if (newSplitOffer)
        {
            string recipient = deal.SplitOfferBy == deal.BuyerWallet ? deal.SellerWallet : deal.BuyerWallet;
            notifications.Add(NewNotification(recipient, deal, NotificationKind.SplitProposed));
        }

        return notifications;
    }

    private static Notification NewNotification(string wallet, Deal deal, NotificationKind kind)
    {
        return new Notification { Wallet = wallet, DealId = deal.Id, Kind = kind };
    }

    private static DealStatus ToOpenStatus(OnchainDealStatus status)
    {
        return status switch
        {
            OnchainDealStatus.AwaitingSeller => DealStatus.AwaitingSeller,
            OnchainDealStatus.Active         => DealStatus.Active,
            OnchainDealStatus.Delivered      => DealStatus.Delivered,
            _                                => DealStatus.Disputed,
        };
    }

    private static DealStatus ToFinalStatus(OnchainOutcome outcome)
    {
        return outcome switch
        {
            OnchainOutcome.Completed      => DealStatus.Completed,
            OnchainOutcome.Split          => DealStatus.Split,
            OnchainOutcome.Undelivered    => DealStatus.Undelivered,
            OnchainOutcome.SellerRefunded => DealStatus.SellerRefunded,
            _                             => DealStatus.Cancelled,
        };
    }

    private static DateTime? FromUnix(long seconds)
    {
        return seconds == 0 ? null : DateTimeOffset.FromUnixTimeSeconds(seconds).UtcDateTime; // 0 = not yet
    }

    private static DealSnapshot Snapshot(Deal deal)
    {
        return new DealSnapshot(deal.Status, deal.MilestonesReleased, deal.SplitOfferBy, deal.SplitOfferBuyerAmount);
    }

    private record DealSnapshot(DealStatus Status, int MilestonesReleased, string? SplitOfferBy, long? SplitOfferBuyerAmount);
}
