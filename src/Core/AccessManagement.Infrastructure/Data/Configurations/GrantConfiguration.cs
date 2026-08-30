using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using AccessManagement.Domain.Authorization;

namespace AccessManagement.Infrastructure.Data.Configurations;

public class GrantConfiguration : IEntityTypeConfiguration<Grant>
{
    public void Configure(EntityTypeBuilder<Grant> builder)
    {
        builder.ToTable("Grant");
        builder.HasKey(g => g.Id);

        builder.Property(g => g.Resource).HasMaxLength(100);
        builder.Property(g => g.ResourceId).HasMaxLength(100);

        builder.Property(g => g.SubjectType).HasConversion<int>();
        builder.Property(g => g.SourceType).HasConversion<int>();
        builder.Property(g => g.Effect).HasConversion<int>();

        builder.HasOne(g => g.Permission)
            .WithMany()
            .HasForeignKey(g => g.PermissionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(g => new { g.SubjectType, g.SubjectUserId, g.PermissionId });
        builder.HasIndex(g => new { g.SubjectType, g.SubjectId, g.PermissionId });
        builder.HasIndex(g => new { g.SourceType, g.SourceId });
        builder.HasIndex(g => g.ScopeUnitId);

        builder.OwnsMany(g => g.Constraints, c =>
        {
            c.ToTable("GrantConstraint");
            c.WithOwner().HasForeignKey("GrantId");
            c.Property(x => x.Kind).HasConversion<int>();
            c.Property(x => x.ScopeKey).HasMaxLength(100);
            c.Property(x => x.ScopeValue).HasMaxLength(100);
        });
    }
}
