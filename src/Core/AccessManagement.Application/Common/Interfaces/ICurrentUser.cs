namespace AccessManagement.Application.Common.Interfaces;

public interface ICurrentUser
{
    Guid? UserId { get; }

    bool IsAuthenticated { get; }

    Guid? PersonnelId { get; }

    /// <summary>
    /// Active company workspace selected by the user (loaded from the database each request).
    /// Position-based grants are resolved only within this company.
    /// </summary>
    Guid? ActiveCompanyId { get; }
}
