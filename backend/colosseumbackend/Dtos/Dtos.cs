using System.ComponentModel.DataAnnotations;
using colosseumbackend.Common;

namespace colosseumbackend.Dtos;

// Token amounts travel as strings of base units: u64 values don't fit safely in a JavaScript number.

#region Shared
public class PageQuery
{
    [Range(1, int.MaxValue)]
    public int PageNumber { get; set; } = 1;

    [Range(1, Constants.MaxPageSize)]
    public int PageSize { get; set; } = Constants.DefaultPageSize;
}

public static class AmountFormat
{
    public const string Pattern = @"^\d{1,18}$"; // fits in a signed 64-bit column
}
#endregion

#region Auth
public class NonceResponseDto
{
    public string Message { get; set; } = string.Empty;
}

public class VerifySignInDto
{
    [Required, MaxLength(Constants.WalletMaxLength)]
    public string Wallet { get; set; } = string.Empty;

    [Required]
    public string Message { get; set; } = string.Empty;

    /// <summary>Base58 ed25519 signature of the UTF-8 message.</summary>
    [Required]
    public string Signature { get; set; } = string.Empty;
}
#endregion

#region Users
public class MinimalUserDto
{
    public string  Wallet      { get; set; } = string.Empty;
    public string? DisplayName { get; set; }
}

public class FullUserDto
{
    public string   Wallet      { get; set; } = string.Empty;
    public string?  DisplayName { get; set; }
    public string?  Discord     { get; set; }
    public DateTime CreatedAt   { get; set; }
}

public class ReputationDto
{
    public uint   DealsCompleted   { get; set; }
    public uint   DealsDisputed    { get; set; }
    public uint   DealsUndelivered { get; set; }
    public string Volume           { get; set; } = "0";
}

public class StandardUserDto
{
    public string                  Wallet         { get; set; } = string.Empty;
    public string?                 DisplayName    { get; set; }
    public string?                 Discord        { get; set; }
    public ReputationDto           Reputation     { get; set; } = new();
    public List<MinimalListingDto> ActiveListings { get; set; } = [];
}

public class EditUserDto
{
    [MaxLength(Constants.DisplayNameMaxLength)]
    public string? DisplayName { get; set; }

    [MaxLength(Constants.DiscordMaxLength)]
    public string? Discord { get; set; }
}
#endregion

#region Games
public class GameDto
{
    public int     Id             { get; set; }
    public string  Slug           { get; set; } = string.Empty;
    public string  Name           { get; set; } = string.Empty;
    public string? IconUrl        { get; set; }
    public int     ActiveListings { get; set; }
}
#endregion

#region Listings
public class ListingSearchQuery : PageQuery
{
    [MaxLength(Constants.GameSlugMaxLength)]
    public string? Game { get; set; }

    public ListingType? Type { get; set; }

    [MaxLength(Constants.TitleMaxLength)]
    public string? Q { get; set; }
}

public class CreateListingDto
{
    [Range(1, int.MaxValue)]
    public int GameId { get; set; }

    public ListingType Type { get; set; }

    [Required, MaxLength(Constants.TitleMaxLength)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(Constants.DescriptionMaxLength)]
    public string Description { get; set; } = string.Empty;

    /// <summary>Total price in token base units.</summary>
    [Required, RegularExpression(AmountFormat.Pattern)]
    public string Price { get; set; } = string.Empty;

    [Range(1, Constants.MaxMilestones)]
    public int Milestones { get; set; } = 1;

    [Required, MaxLength(Constants.UnitLabelMaxLength)]
    public string UnitLabel { get; set; } = string.Empty;

    /// <summary>One of the keys of <see cref="Constants.DeliveryWindowPresets"/>.</summary>
    public string DeliveryWindow { get; set; } = Constants.DefaultDeliveryWindow;
}

public class EditListingDto : CreateListingDto
{
}

public class DepositPreviewDto
{
    public string BuyerDeposit  { get; set; } = "0";
    public string SellerDeposit { get; set; } = "0";
}

public class MinimalListingDto
{
    public Guid           Id         { get; set; }
    public string         Title      { get; set; } = string.Empty;
    public string         GameSlug   { get; set; } = string.Empty;
    public string         GameName   { get; set; } = string.Empty;
    public ListingType    Type       { get; set; }
    public string         Price      { get; set; } = "0";
    public int            Milestones { get; set; }
    public string         UnitLabel  { get; set; } = string.Empty;
    public MinimalUserDto Seller     { get; set; } = new();
}

public class StandardListingDto : MinimalListingDto
{
    public string             Description      { get; set; } = string.Empty;
    public string             DeliveryWindow   { get; set; } = string.Empty;
    public bool               IsActive         { get; set; }
    public DateTime           CreatedAt        { get; set; }
    public ReputationDto      SellerReputation { get; set; } = new();
    /// <summary>What each side would lock if the signed-in caller bought now; null when signed out or for the seller.</summary>
    public DepositPreviewDto? DepositPreview   { get; set; }
}
#endregion

#region Deals
public class DealQuery : PageQuery
{
    /// <summary>"buyer", "seller", or empty for both.</summary>
    [RegularExpression("^(buyer|seller)$")]
    public string? Role { get; set; }

    public DealStatus? Status { get; set; }
}

public class CreateDealDto
{
    public Guid ListingId { get; set; }

    [MaxLength(Constants.BuyerNoteMaxLength)]
    public string? BuyerNote { get; set; }
}

public class DealActionDto
{
    public DealAction Action { get; set; }

    /// <summary>For Confirm: how many parts to release.</summary>
    [Range(1, Constants.MaxMilestones)]
    public int? Parts { get; set; }

    /// <summary>For ProposeSplit: what the buyer gets back, in base units.</summary>
    [RegularExpression(AmountFormat.Pattern)]
    public string? BuyerAmount { get; set; }
}

public class SyncDealDto
{
    [Required, MaxLength(Constants.SignatureMaxLength)]
    public string Signature { get; set; } = string.Empty;
}

public class SplitOfferDto
{
    public string By          { get; set; } = string.Empty;
    public string BuyerAmount { get; set; } = "0";
}

public class MinimalDealDto
{
    public Guid       Id        { get; set; }
    public string     Title     { get; set; } = string.Empty;
    public DealStatus Status    { get; set; }
    public string     Price     { get; set; } = "0";
    public string     Role      { get; set; } = string.Empty;
    public DateTime   CreatedAt { get; set; }
}

public class FullDealDto : MinimalDealDto
{
    public string             DealPda            { get; set; } = string.Empty;
    public string             OnchainDealId      { get; set; } = string.Empty;
    public Guid               ListingId          { get; set; }
    public string             GameName           { get; set; } = string.Empty;
    public string             UnitLabel          { get; set; } = string.Empty;
    public string             BuyerWallet        { get; set; } = string.Empty;
    public string             SellerWallet       { get; set; } = string.Empty;
    public string?            BuyerNote          { get; set; }
    public int                Milestones         { get; set; }
    public int                MilestonesReleased { get; set; }
    public string             BuyerDeposit       { get; set; } = "0";
    public string             SellerDeposit      { get; set; } = "0";
    public long               DeliveryWindowSeconds { get; set; }
    public DateTime?          AcceptedAt         { get; set; }
    public DateTime?          DeliveredAt        { get; set; }
    public DateTime?          DeliveryDeadline   { get; set; }
    public SplitOfferDto?     SplitOffer         { get; set; }
    public string?            LastSignature      { get; set; }
    public List<DealAction>   AllowedActions     { get; set; } = [];
}

public class DealTransactionDto
{
    public FullDealDto Deal        { get; set; } = new();
    /// <summary>Unsigned transaction, base64; the caller's wallet signs and sends it.</summary>
    public string      Transaction { get; set; } = string.Empty;
}
#endregion

#region Notifications
public class NotificationDto
{
    public long             Id        { get; set; }
    public Guid?            DealId    { get; set; }
    public NotificationKind Kind      { get; set; }
    public DateTime         CreatedAt { get; set; }
    public bool             IsRead    { get; set; }
}
#endregion

#region Config
public class ConfigDto
{
    public string Cluster      { get; set; } = string.Empty;
    public string RpcUrl       { get; set; } = string.Empty;
    public string ProgramId    { get; set; } = string.Empty;
    public string Mint         { get; set; } = string.Empty;
    public int    MintDecimals { get; set; }
    /// <summary>Null until the program's config has been initialized on this cluster.</summary>
    public int?   FeeBps       { get; set; }
}
#endregion
