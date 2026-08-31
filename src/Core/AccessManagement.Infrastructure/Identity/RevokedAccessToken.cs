using AccessManagement.Domain.Common;

namespace AccessManagement.Infrastructure.Identity;

public sealed class RevokedAccessToken : BaseEntity
{
    public string Jti { get; set; } = string.Empty;

    public Guid UserId { get; set; }

    public DateTimeOffset ExpiresAtUtc { get; set; }
}
