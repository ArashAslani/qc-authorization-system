using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using qc_authorization.Domain.Authorization;

namespace qc_authorization.Infrastructure.Data.Configurations;

public class PermissionConfiguration : IEntityTypeConfiguration<Permission>
{
    public void Configure(EntityTypeBuilder<Permission> builder)
    {
        builder.ToTable("Permission");
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Code).HasMaxLength(200).IsRequired();
        builder.Property(p => p.Resource).HasMaxLength(100).IsRequired();
        builder.Property(p => p.Action).HasMaxLength(50).IsRequired();
        builder.Property(p => p.Description).HasMaxLength(500);

        builder.HasOne(p => p.ResourceCatalog)
            .WithMany()
            .HasForeignKey(p => p.ResourceCatalogId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(p => p.ActionCatalog)
            .WithMany()
            .HasForeignKey(p => p.ActionCatalogId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(p => p.Code).IsUnique();
    }
}
