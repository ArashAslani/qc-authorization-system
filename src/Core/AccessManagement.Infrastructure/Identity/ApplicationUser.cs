using Microsoft.AspNetCore.Identity;

namespace AccessManagement.Infrastructure.Identity;

public sealed class ApplicationUser : IdentityUser<Guid>
{
    public Guid? PersonnelId { get; set; }
}
