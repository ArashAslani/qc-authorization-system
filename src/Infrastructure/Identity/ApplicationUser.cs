using Microsoft.AspNetCore.Identity;

namespace qc_authorization.Infrastructure.Identity;

public sealed class ApplicationUser : IdentityUser<Guid>
{
    public Guid? PersonnelId { get; set; }
}
