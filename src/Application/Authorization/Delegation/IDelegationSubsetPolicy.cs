using qc_authorization.Application.Authorization.Evaluation;
using qc_authorization.Domain.Authorization.ValueObjects;

namespace qc_authorization.Application.Authorization.Delegation;

public interface IDelegationSubsetPolicy
{
    Task EnsureDelegatorCanDelegateAsync(
        Guid delegatorUserId,
        int permissionId,
        ScopeKind scopeKind,
        string? scopeIdentifier,
        DateTimeOffset when,
        CancellationToken cancellationToken = default);
}
