using System.Globalization;
using colosseumbackend.Common;
using colosseumbackend.Data.Entities;
using colosseumbackend.Dtos;
using colosseumbackend.Services;
using colosseumbackend.Solana;

namespace colosseumbackend.Mappers;

public static class AmountMapper
{
    public static string ToDto(long amount)
    {
        return amount.ToString(CultureInfo.InvariantCulture);
    }

    public static string ToDto(ulong amount)
    {
        return amount.ToString(CultureInfo.InvariantCulture);
    }
}

public static class UserMapper
{
    public static MinimalUserDto ToMinimal(User user)
    {
        return new MinimalUserDto { Wallet = user.Wallet, DisplayName = user.DisplayName };
    }

    public static FullUserDto ToFull(User user)
    {
        return new FullUserDto
        {
            Wallet      = user.Wallet,
            DisplayName = user.DisplayName,
            Discord     = user.Discord,
            CreatedAt   = user.CreatedAt,
        };
    }

    public static ReputationDto ToReputation(ReputationAccount record)
    {
        return new ReputationDto
        {
            DealsCompleted   = record.DealsCompleted,
            DealsDisputed    = record.DealsDisputed,
            DealsUndelivered = record.DealsUndelivered,
            Volume           = AmountMapper.ToDto(record.Volume),
        };
    }

    public static StandardUserDto ToStandard(User user, ReputationAccount record, List<Listing> activeListings)
    {
        return new StandardUserDto
        {
            Wallet         = user.Wallet,
            DisplayName    = user.DisplayName,
            Discord        = user.Discord,
            Reputation     = ToReputation(record),
            ActiveListings = activeListings.Select(ListingMapper.ToMinimal).ToList(),
        };
    }
}

public static class GameMapper
{
    public static GameDto ToDto(Game game, int activeListings)
    {
        return new GameDto
        {
            Id             = game.Id,
            Slug           = game.Slug,
            Name           = game.Name,
            IconUrl        = game.IconUrl,
            ActiveListings = activeListings,
        };
    }
}

public static class ListingMapper
{
    public static Listing ToEntity(string sellerWallet, CreateListingDto createListingDto, long price, TimeSpan deliveryWindow)
    {
        return new Listing
        {
            SellerWallet          = sellerWallet,
            GameId                = createListingDto.GameId,
            Type                  = createListingDto.Type,
            Title                 = createListingDto.Title.Trim(),
            Description           = createListingDto.Description.Trim(),
            Price                 = price,
            Milestones            = createListingDto.Milestones,
            UnitLabel             = createListingDto.UnitLabel.Trim(),
            DeliveryWindowSeconds = (long)deliveryWindow.TotalSeconds,
        };
    }

    public static MinimalListingDto ToMinimal(Listing listing)
    {
        var dto = new MinimalListingDto();
        FillMinimal(dto, listing);
        return dto;
    }

    public static StandardListingDto ToStandard(Listing listing, ReputationAccount sellerRecord, DepositPreviewDto? depositPreview)
    {
        var dto = new StandardListingDto
        {
            Description      = listing.Description,
            DeliveryWindow   = DeliveryWindows.KeyFor(listing.DeliveryWindowSeconds),
            IsActive         = listing.IsActive,
            CreatedAt        = listing.CreatedAt,
            SellerReputation = UserMapper.ToReputation(sellerRecord),
            DepositPreview   = depositPreview,
        };
        FillMinimal(dto, listing);
        return dto;
    }

    private static void FillMinimal(MinimalListingDto dto, Listing listing)
    {
        dto.Id         = listing.Id;
        dto.Title      = listing.Title;
        dto.GameSlug   = listing.Game?.Slug ?? string.Empty;
        dto.GameName   = listing.Game?.Name ?? string.Empty;
        dto.Type       = listing.Type;
        dto.Price      = AmountMapper.ToDto(listing.Price);
        dto.Milestones = listing.Milestones;
        dto.UnitLabel  = listing.UnitLabel;
        dto.Seller     = listing.Seller is null ? new MinimalUserDto { Wallet = listing.SellerWallet } : UserMapper.ToMinimal(listing.Seller);
    }
}

public static class DealMapper
{
    public const string BuyerRole  = "buyer";
    public const string SellerRole = "seller";

    public static MinimalDealDto ToMinimal(Deal deal, string callerWallet)
    {
        var dto = new MinimalDealDto();
        FillMinimal(dto, deal, callerWallet);
        return dto;
    }

    public static FullDealDto ToFull(Deal deal, string callerWallet, List<DealAction> allowedActions)
    {
        var dto = new FullDealDto
        {
            DealPda               = deal.DealPda,
            OnchainDealId         = AmountMapper.ToDto(unchecked((ulong)deal.OnchainDealId)),
            ListingId             = deal.ListingId,
            GameName              = deal.Listing?.Game?.Name ?? string.Empty,
            UnitLabel             = deal.Listing?.UnitLabel ?? string.Empty,
            BuyerWallet           = deal.BuyerWallet,
            SellerWallet          = deal.SellerWallet,
            BuyerNote             = deal.BuyerNote,
            Milestones            = deal.Milestones,
            MilestonesReleased    = deal.MilestonesReleased,
            BuyerDeposit          = AmountMapper.ToDto(deal.BuyerDeposit),
            SellerDeposit         = AmountMapper.ToDto(deal.SellerDeposit),
            DeliveryWindowSeconds = deal.DeliveryWindowSeconds,
            AcceptedAt            = deal.AcceptedAt,
            DeliveredAt           = deal.DeliveredAt,
            DeliveryDeadline      = deal.DeliveryDeadline,
            SplitOffer            = deal.SplitOfferBy is null
                ? null
                : new SplitOfferDto { By = deal.SplitOfferBy, BuyerAmount = AmountMapper.ToDto(deal.SplitOfferBuyerAmount ?? 0) },
            LastSignature         = deal.LastSignature,
            AllowedActions        = allowedActions,
        };
        FillMinimal(dto, deal, callerWallet);
        return dto;
    }

    /// <summary>A new draft from the listing's terms; deposits arrive with the first sync.</summary>
    public static Deal ToDraft(Listing listing, string buyerWallet, ulong onchainDealId, string dealPda, string? buyerNote)
    {
        return new Deal
        {
            OnchainDealId         = unchecked((long)onchainDealId),
            DealPda               = dealPda,
            ListingId             = listing.Id,
            BuyerWallet           = buyerWallet,
            SellerWallet          = listing.SellerWallet,
            BuyerNote             = buyerNote?.Trim(),
            Price                 = listing.Price,
            Milestones            = listing.Milestones,
            DeliveryWindowSeconds = listing.DeliveryWindowSeconds,
        };
    }

    private static void FillMinimal(MinimalDealDto dto, Deal deal, string callerWallet)
    {
        dto.Id        = deal.Id;
        dto.Title     = deal.Listing?.Title ?? string.Empty;
        dto.Status    = deal.Status;
        dto.Price     = AmountMapper.ToDto(deal.Price);
        dto.Role      = deal.BuyerWallet == callerWallet ? BuyerRole : SellerRole;
        dto.CreatedAt = deal.CreatedAt;
    }
}

public static class NotificationMapper
{
    public static NotificationDto ToDto(Notification notification)
    {
        return new NotificationDto
        {
            Id        = notification.Id,
            DealId    = notification.DealId,
            Kind      = notification.Kind,
            CreatedAt = notification.CreatedAt,
            IsRead    = notification.ReadAt is not null,
        };
    }
}
