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
