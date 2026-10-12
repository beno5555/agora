using colosseumbackend.Common;
using colosseumbackend.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace colosseumbackend.Data.Configurations;

public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.Property(notification => notification.Wallet).HasMaxLength(Constants.WalletMaxLength);
        builder.Property(notification => notification.Kind).HasMaxLength(Constants.EnumColumnMaxLength).HasConversion<string>();
        builder.HasIndex(notification => new { notification.Wallet, notification.ReadAt });
    }
}
