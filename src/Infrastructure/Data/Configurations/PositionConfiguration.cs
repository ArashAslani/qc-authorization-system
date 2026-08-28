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

        builder.Property(p => p.Code).HasMaxLength(64).IsRequired();
        builder.Property(p => p.Name).HasMaxLength(200).IsRequired();

        builder.HasIndex(p => p.Code).IsUnique();

        builder.HasOne(p => p.Parent)
            .WithMany(p => p.Children)
            .HasForeignKey(p => p.ParentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(p => p.ParentId);
    }
}
