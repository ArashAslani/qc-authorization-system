using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using qc_authorization.Domain.Organization;
using qc_authorization.Infrastructure.Identity;

namespace qc_authorization.Infrastructure.Data.Configurations;

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
