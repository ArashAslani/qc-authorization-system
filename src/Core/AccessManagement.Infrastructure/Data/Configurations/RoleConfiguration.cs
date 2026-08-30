using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using AccessManagement.Domain.Authorization;

namespace AccessManagement.Infrastructure.Data.Configurations;

public class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.ToTable("Role");
        builder.HasKey(r => r.Id);

        builder.Property(r => r.Code).HasMaxLength(100).IsRequired();
        builder.Property(r => r.Name).HasMaxLength(200).IsRequired();
        builder.Property(r => r.Description).HasMaxLength(500);
        builder.Property(r => r.Status).HasConversion<int>();

        builder.Property(r => r.ParentRoleId);

        builder.HasIndex(r => r.Code).IsUnique();

        builder.HasOne<Role>()
            .WithMany()
            .HasForeignKey(r => r.ParentRoleId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
