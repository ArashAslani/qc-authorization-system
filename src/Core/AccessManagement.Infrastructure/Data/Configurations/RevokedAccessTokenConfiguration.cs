using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using AccessManagement.Infrastructure.Identity;

namespace AccessManagement.Infrastructure.Data.Configurations;

public sealed class RevokedAccessTokenConfiguration : IEntityTypeConfiguration<RevokedAccessToken>
{
    public void Configure(EntityTypeBuilder<RevokedAccessToken> builder)
    {
        builder.ToTable("RevokedAccessTokens");
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Jti).HasMaxLength(64).IsRequired();
        builder.HasIndex(t => t.Jti).IsUnique();
        builder.HasIndex(t => t.ExpiresAtUtc);
    }
}
