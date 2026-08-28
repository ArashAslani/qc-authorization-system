using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using qc_authorization.Domain.Organization;

namespace qc_authorization.Infrastructure.Data.Configurations;

public class PositionConfiguration : IEntityTypeConfiguration<Position>
{
    public void Configure(EntityTypeBuilder<Position> builder)
    {
        builder.ToTable("Position");
        builder.HasKey(p => p.Id);

        builder.Property(p => p.CompanyId).IsRequired();
        builder.Property(p => p.Code).HasMaxLength(64).IsRequired();
        builder.Property(p => p.Title).HasMaxLength(200).IsRequired();
        builder.Property(p => p.Description).HasMaxLength(500);
        builder.Property(p => p.Status).HasConversion<int>();

        builder.HasIndex(p => new { p.CompanyId, p.Code }).IsUnique();

        builder.HasOne(p => p.Parent)
            .WithMany(p => p.Children)
            .HasForeignKey(p => p.ParentPositionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(p => p.ParentPositionId);
        builder.HasIndex(p => p.CompanyId);
    }
}
