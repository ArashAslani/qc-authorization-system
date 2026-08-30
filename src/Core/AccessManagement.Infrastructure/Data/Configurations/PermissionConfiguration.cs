using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using AccessManagement.Domain.Authorization;

namespace AccessManagement.Infrastructure.Data.Configurations;

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
        builder.Property(p => p.PluginCode).HasMaxLength(50).IsRequired().HasDefaultValue(CoreAccessPermissions.PluginCode);

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
