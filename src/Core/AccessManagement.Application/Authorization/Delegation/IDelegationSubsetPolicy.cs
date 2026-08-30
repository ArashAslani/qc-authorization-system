namespace AccessManagement.Application.Authorization.Delegation;

public interface IDelegationSubsetPolicy
{
    Task EnsureDelegatorCanDelegateAsync(
        Guid delegatorUserId,
        Guid permissionId,
        Guid? scopeUnitId,
        DateTimeOffset when,
        CancellationToken cancellationToken = default);
}
