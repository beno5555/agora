using colosseumbackend.Common;
using colosseumbackend.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace colosseumbackend.Data.Configurations;

public class DealConfiguration : IEntityTypeConfiguration<Deal>
{
    public void Configure(EntityTypeBuilder<Deal> builder)
    {
        builder.Property(deal => deal.DealPda).HasMaxLength(Constants.WalletMaxLength);
        builder.Property(deal => deal.BuyerWallet).HasMaxLength(Constants.WalletMaxLength);
        builder.Property(deal => deal.SellerWallet).HasMaxLength(Constants.WalletMaxLength);
        builder.Property(deal => deal.SplitOfferBy).HasMaxLength(Constants.WalletMaxLength);
        builder.Property(deal => deal.BuyerNote).HasMaxLength(Constants.BuyerNoteMaxLength);
        builder.Property(deal => deal.LastSignature).HasMaxLength(Constants.SignatureMaxLength);
        builder.Property(deal => deal.Status).HasMaxLength(Constants.EnumColumnMaxLength).HasConversion<string>();
        builder.Ignore(deal => deal.DeliveryDeadline);
        builder.Ignore(deal => deal.ReviewDeadline);

        builder.HasIndex(deal => deal.DealPda).IsUnique();
        builder.HasIndex(deal => deal.BuyerWallet);
        builder.HasIndex(deal => deal.SellerWallet);
        builder.HasIndex(deal => deal.Status);

        // Listings are never deleted (only paused), so deals keep their listing.
        builder.HasOne(deal => deal.Listing).WithMany().HasForeignKey(deal => deal.ListingId).OnDelete(DeleteBehavior.Restrict);
    }
}
