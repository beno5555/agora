using colosseumbackend.Common;

namespace colosseumbackend.Data.Entities;

public class Deal
{
    public Guid       Id                    { get; set; } = Guid.NewGuid();
    /// <summary>The program's u64 deal_id, stored bit-cast.</summary>
    public long       OnchainDealId         { get; set; }
    public string     DealPda               { get; set; } = string.Empty;
    public Guid       ListingId             { get; set; }
    public string     BuyerWallet           { get; set; } = string.Empty;
    public string     SellerWallet          { get; set; } = string.Empty;
    public string?    BuyerNote             { get; set; }
    public DealStatus Status                { get; set; } = DealStatus.Draft;

    // Copied from the on-chain account; the chain is the source of truth.
    public long       Price                 { get; set; }
    public int        Milestones            { get; set; }
    public int        MilestonesReleased    { get; set; }
    public long       BuyerDeposit          { get; set; }
    public long       SellerDeposit         { get; set; }
    public long       DeliveryWindowSeconds { get; set; }
    public DateTime?  AcceptedAt            { get; set; }
    public DateTime?  DeliveredAt           { get; set; }
    public string?    SplitOfferBy          { get; set; }
    public long?      SplitOfferBuyerAmount { get; set; }

    public string?    LastSignature         { get; set; }
    /// <summary>Set once the "deadline soon" reminder went out, so it's sent only once.</summary>
    public bool       DeadlineReminderSent  { get; set; }
    /// <summary>Set once the buyer was told a refund is available, so it's sent only once.</summary>
    public bool       RefundNoticeSent      { get; set; }
    public DateTime   CreatedAt             { get; set; } = DateTime.UtcNow;
    public DateTime?  SyncedAt              { get; set; }

    public Listing? Listing { get; set; }

    public DateTime? DeliveryDeadline => AcceptedAt?.AddSeconds(DeliveryWindowSeconds);
}
