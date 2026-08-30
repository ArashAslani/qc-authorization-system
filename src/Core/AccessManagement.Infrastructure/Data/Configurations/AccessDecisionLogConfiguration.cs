using AccessManagement.Domain.Authorization.Audit;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AccessManagement.Infrastructure.Data.Configurations;

public class AccessDecisionLogConfiguration : IEntityTypeConfiguration<AccessDecisionLog>
{
    public void Configure(EntityTypeBuilder<AccessDecisionLog> builder)
    {
        builder.ToTable("AccessDecisionLog");
        builder.HasKey(l => l.Id);
        builder.Property(l => l.PermissionCode).HasMaxLength(150).IsRequired();
        builder.Property(l => l.ResourceId).HasMaxLength(100);
        builder.Property(l => l.Decision).HasMaxLength(10).IsRequired();
        builder.Property(l => l.Reason).HasMaxLength(50).IsRequired();
        builder.HasIndex(l => l.RequestedByUserId);
        builder.HasIndex(l => l.CreatedAt);
        builder.HasIndex(l => l.TraceId);
    }
}
