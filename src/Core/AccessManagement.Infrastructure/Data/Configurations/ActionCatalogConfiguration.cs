using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using AccessManagement.Domain.Authorization;

namespace AccessManagement.Infrastructure.Data.Configurations;

public class ActionCatalogConfiguration : IEntityTypeConfiguration<ActionCatalog>
{
    public void Configure(EntityTypeBuilder<ActionCatalog> builder)
    {
        builder.ToTable("ActionCatalog");
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Code).HasMaxLength(50).IsRequired();
        builder.Property(a => a.Name).HasMaxLength(100).IsRequired();
        builder.HasIndex(a => a.Code).IsUnique();
    }
}
