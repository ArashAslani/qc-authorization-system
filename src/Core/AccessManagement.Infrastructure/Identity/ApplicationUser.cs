using Microsoft.AspNetCore.Identity;

namespace AccessManagement.Infrastructure.Identity;

public sealed class ApplicationUser : IdentityUser<Guid>
{
    public Guid? PersonnelId { get; set; }

    /// <summary>
    /// Persisted active company workspace. Request evaluation reads this from the database,
    /// not from a JWT claim.
    /// </summary>
    public Guid? ActiveCompanyId { get; set; }
}
