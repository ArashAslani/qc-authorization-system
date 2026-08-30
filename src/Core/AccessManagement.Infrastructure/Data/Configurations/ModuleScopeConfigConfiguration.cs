using AccessManagement.Domain.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AccessManagement.Infrastructure.Data.Configurations;

public class ModuleScopeConfigConfiguration : IEntityTypeConfiguration<ModuleScopeConfig>
{
    public void Configure(EntityTypeBuilder<ModuleScopeConfig> builder)
    {
        builder.ToTable("ModuleScopeConfig");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.ResourceCode).HasMaxLength(100).IsRequired();
        builder.Property(c => c.MaxScopeUnitType).HasMaxLength(50).IsRequired();
        builder.HasIndex(c => c.ResourceCode).IsUnique();
    }
}
