namespace AccessManagement.Application.Authorization.Delegation;

public interface IDelegationSubsetPolicy
{
    Task EnsureDelegatorCanDelegateAsync(
        Guid delegatorUserId,
        Guid permissionId,
        Guid? scopeUnitId,
        DateTimeOffset when,
        Guid? delegatorCompanyUnitId = null,
        CancellationToken cancellationToken = default);
}
