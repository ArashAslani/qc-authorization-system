using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using qc_authorization.Domain.Organization;

namespace qc_authorization.Infrastructure.Data.Configurations;

public class PersonnelConfiguration : IEntityTypeConfiguration<Personnel>
{
    public void Configure(EntityTypeBuilder<Personnel> builder)
    {
        builder.ToTable("Personnel");
        builder.HasKey(p => p.Id);

        builder.Property(p => p.FirstName).HasMaxLength(100).IsRequired();
        builder.Property(p => p.LastName).HasMaxLength(100).IsRequired();
        builder.Property(p => p.Email).HasMaxLength(256);

        builder.HasIndex(p => p.Email).IsUnique().HasFilter(null);

        builder.HasMany(p => p.Assignments)
            .WithOne(a => a.Personnel)
            .HasForeignKey(a => a.PersonnelId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
