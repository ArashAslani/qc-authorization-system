using Microsoft.AspNetCore.Identity;

namespace qc_authorization.Infrastructure.Identity;

/// <summary>
/// ASP.NET Identity role for authentication membership only.
/// Qc authorization roles live in <see cref="Domain.Authorization.Role"/>.
/// </summary>
public sealed class ApplicationRole : IdentityRole<Guid>
{
}
