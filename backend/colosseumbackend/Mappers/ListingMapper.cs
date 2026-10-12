using colosseumbackend.Data.Entities;
using colosseumbackend.Dtos;
using colosseumbackend.Services;
using colosseumbackend.Solana;

namespace colosseumbackend.Mappers;

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
