using AccessManagement.Domain.Organization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AccessManagement.Infrastructure.Data.Configurations;

public class OrganizationalUnitConfiguration : IEntityTypeConfiguration<OrganizationalUnit>
{
    public void Configure(EntityTypeBuilder<OrganizationalUnit> builder)
    {
        builder.ToTable("OrganizationalUnit");
        builder.HasKey(u => u.Id);

        builder.Property(u => u.UnitType).HasMaxLength(50).IsRequired();
        builder.Property(u => u.Name).HasMaxLength(200).IsRequired();

        builder.HasOne(u => u.Parent)
            .WithMany(u => u.Children)
            .HasForeignKey(u => u.ParentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(u => u.ParentId);
        builder.HasIndex(u => u.UnitType);
    }
}
