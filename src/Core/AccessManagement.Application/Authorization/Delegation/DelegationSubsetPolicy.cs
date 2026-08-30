using AccessManagement.Application.Abstractions;
using AccessManagement.Application.Common.Interfaces;
using AccessManagement.Domain.Authorization.Enums;
using AccessManagement.Domain.Authorization.Evaluation;
using AccessManagement.Domain.Authorization.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace AccessManagement.Application.Authorization.Delegation;

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
        Guid? scopeUnitId,
        DateTimeOffset when,
        CancellationToken cancellationToken = default)
    {
        var permission = await _context.Permissions
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == permissionId, cancellationToken)
            ?? throw new InvalidOperationException($"Permission {permissionId} not found.");

        var request = AccessRequest.ForUser(
            delegatorUserId,
            permission.Code,
            resourceScopeUnitId: scopeUnitId,
            when: when);

        var decision = await _evaluator.EvaluateAsync(request, cancellationToken);
        if (decision.Effect != Effect.Allow)
        {
            throw new AuthorizationDomainException(
                "Delegator does not have effective access required to delegate this permission.");
        }
    }
}
