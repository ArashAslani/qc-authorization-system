using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using AccessManagement.Domain.Organization;

namespace AccessManagement.Infrastructure.Data.Configurations;

public class PositionAssignmentConfiguration : IEntityTypeConfiguration<PositionAssignment>
{
    public void Configure(EntityTypeBuilder<PositionAssignment> builder)
    {
        builder.ToTable("PositionAssignment");
        builder.HasKey(a => a.Id);

        builder.HasOne(a => a.Personnel)
            .WithMany(p => p.Assignments)
            .HasForeignKey(a => a.PersonnelId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(a => a.Position)
            .WithMany()
            .HasForeignKey(a => a.PositionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(a => new { a.PersonnelId, a.PositionId, a.ValidFrom });

        builder.Property(a => a.IsPrimary).IsRequired().HasDefaultValue(false);
    }
}
