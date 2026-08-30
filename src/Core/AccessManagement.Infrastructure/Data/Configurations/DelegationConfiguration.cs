using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using AccessManagement.Domain.Authorization;

namespace AccessManagement.Infrastructure.Data.Configurations;

public class DelegationConfiguration : IEntityTypeConfiguration<Delegation>
{
    public void Configure(EntityTypeBuilder<Delegation> builder)
    {
        builder.ToTable("Delegation");
        builder.HasKey(d => d.Id);

        builder.HasOne(d => d.Permission)
            .WithMany()
            .HasForeignKey(d => d.PermissionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(d => new { d.DelegateUserId, d.PermissionId });
        builder.HasIndex(d => d.DelegatorUserId);
        builder.HasIndex(d => d.ScopeUnitId);
    }
}
