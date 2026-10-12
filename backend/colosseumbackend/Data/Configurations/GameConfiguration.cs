using colosseumbackend.Common;
using colosseumbackend.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace colosseumbackend.Data.Configurations;

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
