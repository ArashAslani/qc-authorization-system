using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using qc_authorization.Domain.Authorization;

namespace qc_authorization.Infrastructure.Data.Configurations;

public class GrantConfiguration : IEntityTypeConfiguration<Grant>
{
    public void Configure(EntityTypeBuilder<Grant> builder)
    {
        builder.ToTable("Grant");
        builder.HasKey(g => g.Id);

        builder.Property(g => g.Resource).HasMaxLength(100);
        builder.Property(g => g.ResourceId).HasMaxLength(100);
        builder.Property(g => g.ScopeIdentifier).HasMaxLength(100);

        builder.Property(g => g.SubjectType).HasConversion<int>();
        builder.Property(g => g.SourceType).HasConversion<int>();
        builder.Property(g => g.Effect).HasConversion<int>();
        builder.Property(g => g.ScopeKind).HasConversion<int>();

        builder.HasOne(g => g.Permission)
            .WithMany()
            .HasForeignKey(g => g.PermissionId)
            .OnDelete(DeleteBehavior.Restrict);

        // Trace-friendly index: lookups by (subject, permission) and by
        // (source type, source id) for revoke/audit.
        builder.HasIndex(g => new { g.SubjectType, g.SubjectId, g.PermissionId });
        builder.HasIndex(g => new { g.SourceType, g.SourceId });
    }
}
