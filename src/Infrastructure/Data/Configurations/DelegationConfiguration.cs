using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using qc_authorization.Domain.Authorization;

namespace qc_authorization.Infrastructure.Data.Configurations;

public class DelegationConfiguration : IEntityTypeConfiguration<Delegation>
{
    public void Configure(EntityTypeBuilder<Delegation> builder)
    {
        builder.ToTable("Delegation");
        builder.HasKey(d => d.Id);

        builder.Property(d => d.ScopeIdentifier).HasMaxLength(100);
        builder.Property(d => d.ScopeKind).HasConversion<int>();

        builder.HasOne(d => d.Permission)
            .WithMany()
            .HasForeignKey(d => d.PermissionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(d => new { d.DelegateUserId, d.PermissionId });
        builder.HasIndex(d => d.DelegatorUserId);
    }
}
