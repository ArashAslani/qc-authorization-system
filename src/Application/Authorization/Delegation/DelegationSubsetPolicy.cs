using qc_authorization.Application.Authorization.Evaluation;
using qc_authorization.Application.Common.Interfaces;
using qc_authorization.Domain.Authorization.Enums;
using qc_authorization.Domain.Authorization.Evaluation;
using qc_authorization.Domain.Authorization.Exceptions;
using qc_authorization.Domain.Authorization.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace qc_authorization.Application.Authorization.Delegation;

public sealed class DelegationSubsetPolicy : IDelegationSubsetPolicy
{
    private readonly IAccessEvaluator _evaluator;
    private readonly IApplicationDbContext _context;

    public DelegationSubsetPolicy(IAccessEvaluator evaluator, IApplicationDbContext context)
    {
        _evaluator = evaluator;
        _context = context;
    }

    public async Task EnsureDelegatorCanDelegateAsync(
        Guid delegatorUserId,
        Guid permissionId,
        ScopeKind scopeKind,
        string? scopeIdentifier,
        DateTimeOffset when,
        CancellationToken cancellationToken = default)
    {
        var permission = await _context.Permissions
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == permissionId, cancellationToken)
            ?? throw new InvalidOperationException($"Permission {permissionId} not found.");

        var resourceId = scopeKind == ScopeKind.Unbounded ? null : scopeIdentifier;
        IReadOnlyDictionary<string, object>? context = scopeKind == ScopeKind.Unbounded || scopeIdentifier is null
            ? null
            : new Dictionary<string, object> { ["Scope"] = scopeIdentifier };

        var request = AccessRequest.ForUser(
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
