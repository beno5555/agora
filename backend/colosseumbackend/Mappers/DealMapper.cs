using colosseumbackend.Common;
using colosseumbackend.Data.Entities;
using colosseumbackend.Dtos;

namespace colosseumbackend.Mappers;

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
