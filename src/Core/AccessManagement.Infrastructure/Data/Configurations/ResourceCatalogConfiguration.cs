using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using AccessManagement.Domain.Authorization;

namespace AccessManagement.Infrastructure.Data.Configurations;

public class ResourceCatalogConfiguration : IEntityTypeConfiguration<ResourceCatalog>
{
    public void Configure(EntityTypeBuilder<ResourceCatalog> builder)
    {
        builder.ToTable("ResourceCatalog");
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Code).HasMaxLength(100).IsRequired();
        builder.Property(r => r.Name).HasMaxLength(200).IsRequired();
        builder.HasIndex(r => r.Code).IsUnique();
    }
}
