namespace qc_authorization.Application.Common.Interfaces;

public interface ICurrentUser
{
    Guid? UserId { get; }

    bool IsAuthenticated { get; }

    int? PersonnelId { get; }
}
