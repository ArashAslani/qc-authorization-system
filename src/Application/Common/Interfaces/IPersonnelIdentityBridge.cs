namespace qc_authorization.Application.Common.Interfaces;

/// <summary>
/// Keeps <see cref="Domain.Organization.Personnel.IdentityUserId"/> and
/// <c>ApplicationUser.PersonnelId</c> in sync.
/// </summary>
public interface IPersonnelIdentityBridge
{
    Task LinkAsync(Guid personnelId, Guid identityUserId, CancellationToken cancellationToken = default);
}
