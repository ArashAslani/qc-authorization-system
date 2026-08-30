using AccessManagement.Application.Common.Interfaces;

namespace AccessManagement.Application.Common.Interfaces;

/// <summary>
/// Test/dev implementation of <see cref="ICurrentUser"/> with configurable workspace context.
/// </summary>
public sealed class StaticCurrentUser : ICurrentUser
{
    public StaticCurrentUser(Guid? userId = null, Guid? personnelId = null, Guid? activeCompanyId = null)
    {
        UserId = userId;
        PersonnelId = personnelId;
        ActiveCompanyId = activeCompanyId;
        IsAuthenticated = userId.HasValue;
    }

    public Guid? UserId { get; set; }

    public bool IsAuthenticated { get; set; }

    public Guid? PersonnelId { get; set; }

    public Guid? ActiveCompanyId { get; set; }
}
