using colosseumbackend.Common;
using colosseumbackend.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace colosseumbackend.Data.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.HasKey(user => user.Wallet);
        builder.Property(user => user.Wallet).HasMaxLength(Constants.WalletMaxLength);
        builder.Property(user => user.DisplayName).HasMaxLength(Constants.DisplayNameMaxLength);
        builder.Property(user => user.Discord).HasMaxLength(Constants.DiscordMaxLength);
    }
}

public class AuthNonceConfiguration : IEntityTypeConfiguration<AuthNonce>
{
    public void Configure(EntityTypeBuilder<AuthNonce> builder)
    {
        // One pending sign-in per wallet; a new request replaces the old one.
        builder.HasKey(nonce => nonce.Wallet);
        builder.Property(nonce => nonce.Wallet).HasMaxLength(Constants.WalletMaxLength);
    }
}

public class GameConfiguration : IEntityTypeConfiguration<Game>
{
    public void Configure(EntityTypeBuilder<Game> builder)
    {
        builder.HasIndex(game => game.Slug).IsUnique();
        builder.Property(game => game.Slug).HasMaxLength(Constants.GameSlugMaxLength);
        builder.Property(game => game.Name).HasMaxLength(Constants.GameNameMaxLength);
        builder.Property(game => game.IconUrl).HasMaxLength(Constants.UrlMaxLength);

        builder.HasData(
            new Game { Id = 1, Slug = "fortnite",          Name = "Fortnite" },
            new Game { Id = 2, Slug = "valorant",          Name = "Valorant" },
            new Game { Id = 3, Slug = "league-of-legends", Name = "League of Legends" },
            new Game { Id = 4, Slug = "cs2",               Name = "Counter-Strike 2" },
            new Game { Id = 5, Slug = "apex-legends",      Name = "Apex Legends" },
            new Game { Id = 6, Slug = "world-of-warcraft", Name = "World of Warcraft" });
    }
}

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

        builder.HasIndex(deal => deal.DealPda).IsUnique();
        builder.HasIndex(deal => deal.BuyerWallet);
        builder.HasIndex(deal => deal.SellerWallet);
        builder.HasIndex(deal => deal.Status);

        // Listings are never deleted (only paused), so deals keep their listing.
        builder.HasOne(deal => deal.Listing).WithMany().HasForeignKey(deal => deal.ListingId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.Property(notification => notification.Wallet).HasMaxLength(Constants.WalletMaxLength);
        builder.Property(notification => notification.Kind).HasMaxLength(Constants.EnumColumnMaxLength).HasConversion<string>();
        builder.HasIndex(notification => new { notification.Wallet, notification.ReadAt });
    }
}

public class ActionLogConfiguration : IEntityTypeConfiguration<ActionLog>
{
    public void Configure(EntityTypeBuilder<ActionLog> builder)
    {
        builder.Property(log => log.Action).HasMaxLength(Constants.LogActionMaxLength);
        builder.Property(log => log.Entity).HasMaxLength(Constants.LogEntityMaxLength);
        builder.Property(log => log.EntityId).HasMaxLength(Constants.LogEntityMaxLength);
        builder.Property(log => log.Wallet).HasMaxLength(Constants.WalletMaxLength);
        builder.Property(log => log.Details).HasMaxLength(Constants.LogDetailsMaxLength);
    }
}
