using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using AccessManagement.Domain.Organization;

namespace AccessManagement.Infrastructure.Data.Configurations;

public class PersonnelConfiguration : IEntityTypeConfiguration<Personnel>
{
    public void Configure(EntityTypeBuilder<Personnel> builder)
    {
        builder.ToTable("Personnel");
        builder.HasKey(p => p.Id);

        builder.Property(p => p.NationalId).HasMaxLength(20);
        builder.Property(p => p.FirstName).HasMaxLength(100).IsRequired();
        builder.Property(p => p.LastName).HasMaxLength(100).IsRequired();
        builder.Property(p => p.PersonnelCode).HasMaxLength(30);
        builder.Property(p => p.PhoneNumber).HasMaxLength(32);
        builder.Property(p => p.Gender).HasConversion<int>();
        builder.Property(p => p.Status).HasConversion<int>();
        builder.Property(p => p.IsSystemUser).IsRequired();

        builder.HasIndex(p => p.NationalId).IsUnique();
        builder.HasIndex(p => p.PersonnelCode).IsUnique();
        builder.HasIndex(p => p.IdentityUserId).IsUnique();

        builder.HasMany(p => p.Assignments)
            .WithOne(a => a.Personnel)
            .HasForeignKey(a => a.PersonnelId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
