using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using AccessManagement.Domain.Authorization.Audit;

namespace AccessManagement.Infrastructure.Data.Configurations;

public class AuthorizationAuditEntryConfiguration : IEntityTypeConfiguration<AuthorizationAuditEntry>
{
    public void Configure(EntityTypeBuilder<AuthorizationAuditEntry> builder)
    {
        builder.ToTable("AuthorizationAuditEntry");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.EventType).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Payload).IsRequired();
        builder.HasIndex(x => x.EventType);
        builder.HasIndex(x => x.Created);
    }
}
