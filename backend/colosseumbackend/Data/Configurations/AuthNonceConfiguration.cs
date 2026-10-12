using colosseumbackend.Common;
using colosseumbackend.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace colosseumbackend.Data.Configurations;

public class AuthNonceConfiguration : IEntityTypeConfiguration<AuthNonce>
{
    public void Configure(EntityTypeBuilder<AuthNonce> builder)
    {
        // One pending sign-in per wallet; a new request replaces the old one.
        builder.HasKey(nonce => nonce.Wallet);
        builder.Property(nonce => nonce.Wallet).HasMaxLength(Constants.WalletMaxLength);
    }
}
