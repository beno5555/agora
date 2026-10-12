using colosseumbackend.Common;
using colosseumbackend.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace colosseumbackend.Data.Configurations;

public class ListingConfiguration : IEntityTypeConfiguration<Listing>
{
    public void Configure(EntityTypeBuilder<Listing> builder)
    {
        builder.Property(listing => listing.SellerWallet).HasMaxLength(Constants.WalletMaxLength);
        builder.Property(listing => listing.Title).HasMaxLength(Constants.TitleMaxLength);
        builder.Property(listing => listing.Description).HasMaxLength(Constants.DescriptionMaxLength);
        builder.Property(listing => listing.UnitLabel).HasMaxLength(Constants.UnitLabelMaxLength);
        builder.Property(listing => listing.Type).HasMaxLength(Constants.EnumColumnMaxLength).HasConversion<string>();

        builder.HasOne(listing => listing.Seller).WithMany().HasForeignKey(listing => listing.SellerWallet);
        builder.HasOne(listing => listing.Game).WithMany().HasForeignKey(listing => listing.GameId);

        builder.HasIndex(listing => new { listing.GameId, listing.IsActive });
        builder.HasIndex(listing => listing.SellerWallet);
    }
}
