using qc_authorization.Application.Authorization.Evaluation;
using qc_authorization.Application.Common.Interfaces.Repositories;
using qc_authorization.Domain.Authorization.Enums;
using qc_authorization.Domain.Authorization.Evaluation;
using qc_authorization.Domain.Authorization.Exceptions;
using qc_authorization.Domain.Authorization.ValueObjects;

namespace qc_authorization.Application.Authorization.Delegation;

public sealed class DelegationSubsetPolicy : IDelegationSubsetPolicy
{
    private readonly IAccessEvaluator _evaluator;
    private readonly IPermissionRepository _permissions;

    public DelegationSubsetPolicy(IAccessEvaluator evaluator, IPermissionRepository permissions)
    {
        _evaluator = evaluator;
        _permissions = permissions;
    }

    public async Task EnsureDelegatorCanDelegateAsync(
        int delegatorUserId,
        int permissionId,
        ScopeKind scopeKind,
        string? scopeIdentifier,
        DateTimeOffset when,
        CancellationToken cancellationToken = default)
    {
        var permission = await _permissions.GetByIdAsync(permissionId, cancellationToken)
            ?? throw new InvalidOperationException($"Permission {permissionId} not found.");

        var resourceId = scopeKind == ScopeKind.Unbounded ? null : scopeIdentifier;
        IReadOnlyDictionary<string, object>? context = scopeKind == ScopeKind.Unbounded || scopeIdentifier is null
            ? null
            : new Dictionary<string, object> { ["Scope"] = scopeIdentifier };

        var request = new AccessRequest(
            SubjectType.User,
            delegatorUserId,
            permission.Action,
            permission.Resource,
            resourceId,
            when,
            context);

        var decision = await _evaluator.EvaluateAsync(request, cancellationToken);
        if (decision.Effect != Effect.Allow)
        {
            throw new AuthorizationDomainException(
                "Delegator does not have effective access required to delegate this permission.");
        }
    }
}
