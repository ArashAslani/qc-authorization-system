using qc_authorization.Domain.Authorization.ValueObjects;

namespace qc_authorization.Application.Authorization.Delegation;

public interface IDelegationSubsetPolicy
{
    Task EnsureDelegatorCanDelegateAsync(
        int delegatorUserId,
        int permissionId,
        ScopeKind scopeKind,
        string? scopeIdentifier,
        DateTimeOffset when,
        CancellationToken cancellationToken = default);
}
