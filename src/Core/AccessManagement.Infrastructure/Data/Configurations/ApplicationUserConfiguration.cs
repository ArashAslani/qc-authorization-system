using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using AccessManagement.Domain.Organization;
using AccessManagement.Infrastructure.Identity;

namespace AccessManagement.Infrastructure.Data.Configurations;

public class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
{
    public void Configure(EntityTypeBuilder<ApplicationUser> builder)
    {
        builder.Property(u => u.PersonnelId);

        builder.HasOne<Personnel>()
            .WithMany()
            .HasForeignKey(u => u.PersonnelId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
