using colosseumbackend.Common;
using colosseumbackend.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace colosseumbackend.Data.Configurations;

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
