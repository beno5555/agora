using colosseumbackend.Common;

namespace colosseumbackend.Data.Entities;

public class User
{
    /// <summary>Base58 wallet address; the account identity everywhere.</summary>
    public string         Wallet      { get; set; } = string.Empty;
    public string?        DisplayName { get; set; }
    public string?        Discord     { get; set; }
    public DateTime       CreatedAt   { get; set; } = DateTime.UtcNow;
}

public class AuthNonce
{
    public string   Wallet    { get; set; } = string.Empty;
    public string   Message   { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
}

public class Game
{
    public int     Id      { get; set; }
    public string  Slug    { get; set; } = string.Empty;
    public string  Name    { get; set; } = string.Empty;
    public string? IconUrl { get; set; }
}

public class Listing
{
    public Guid        Id                    { get; set; } = Guid.NewGuid();
    public string      SellerWallet          { get; set; } = string.Empty;
    public int         GameId                { get; set; }
    public ListingType Type                  { get; set; }
    public string      Title                 { get; set; } = string.Empty;
    public string      Description           { get; set; } = string.Empty;
    /// <summary>Total price in token base units.</summary>
    public long        Price                 { get; set; }
    public int         Milestones            { get; set; }
    public string      UnitLabel             { get; set; } = string.Empty;
    public long        DeliveryWindowSeconds { get; set; }
    public bool        IsActive              { get; set; } = true;
    public DateTime    CreatedAt             { get; set; } = DateTime.UtcNow;

    public User? Seller { get; set; }
    public Game? Game   { get; set; }
}

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

public class Notification
{
    public long             Id        { get; set; }
    public string           Wallet    { get; set; } = string.Empty;
    public Guid?            DealId    { get; set; }
    public NotificationKind Kind      { get; set; }
    public DateTime         CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime?        ReadAt    { get; set; } // null = unread
}

public class ActionLog
{
    public long     Id        { get; set; }
    public string   Action    { get; set; } = string.Empty;
    public string   Entity    { get; set; } = string.Empty;
    public string?  EntityId  { get; set; }
    public bool     Succeeded { get; set; }
    /// <summary>Wallet of the caller; null for background jobs.</summary>
    public string?  Wallet    { get; set; }
    public string?  Details   { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
