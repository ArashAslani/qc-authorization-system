using AccessManagement.Application.Abstractions;
using AccessManagement.Domain.Authorization;
using AccessManagement.Domain.Authorization.Constraints;
using AccessManagement.Domain.Authorization.Enums;
using AccessManagement.Domain.Authorization.Evaluation;

namespace AccessManagement.Application.Authorization.Evaluation;

public sealed class AccessEvaluator : IAccessEvaluator
{
    private readonly IGrantResolver _grantResolver;
    private readonly IScopeMatcher _scopeMatcher;
    private readonly IDecisionTraceWriter _traceWriter;

    public AccessEvaluator(
        IGrantResolver grantResolver,
        IScopeMatcher scopeMatcher,
        IDecisionTraceWriter traceWriter)
    {
        _grantResolver = grantResolver;
        _scopeMatcher = scopeMatcher;
        _traceWriter = traceWriter;
    }

    public async Task<AccessDecision> EvaluateAsync(AccessRequest request, CancellationToken ct = default)
    {
        var candidates = await _grantResolver.FindCandidatesAsync(request, ct);
        var now = request.When;
        var valid = candidates.Where(g => g.Validity.IsActiveAt(now)).ToList();
        var inScope = await FilterInScopeAsync(valid, request, ct);
        var traceId = Guid.NewGuid();

        if (inScope.Count == 0)
        {
            var reason = candidates.Count == 0 || valid.Count == 0
                ? (candidates.Count == 0 ? AccessDecisionReasons.NoGrant : AccessDecisionReasons.Expired)
                : AccessDecisionReasons.OutOfScope;
            var denied = AccessDecision.Deny(traceId, reason);
            return await _traceWriter.WriteAsync(request, candidates, null, denied, ct);
        }

        var winner = PickWinner(inScope);
        var decision = winner.Effect == Effect.Deny
            ? AccessDecision.Deny(traceId, AccessDecisionReasons.Overridden + "_BY_" + winner.SourceType)
            : AccessDecision.Allow(traceId);

        return await _traceWriter.WriteAsync(request, candidates, winner, decision, ct);
    }

    public async Task<AccessibleScopeResult> GetAccessibleScopesAsync(
        Guid subjectUserId,
        Guid? activePositionId,
        string permissionCode,
        CancellationToken ct = default)
    {
        var request = new AccessRequest(subjectUserId, activePositionId, permissionCode, null, DateTimeOffset.UtcNow);
        var candidates = await _grantResolver.FindCandidatesAsync(request, ct);
        var valid = candidates.Where(g => g.Validity.IsActiveAt(request.When)).ToList();

        if (valid.Count == 0)
        {
            return new AccessibleScopeResult(false, Array.Empty<Guid>(), Array.Empty<Guid>());
        }

        var unrestrictedProbe = await FilterInScopeAsync(valid, request with { ResourceScopeUnitId = Guid.NewGuid() }, ct);
        var isUnrestricted = unrestrictedProbe.Count > 0 && PickWinner(unrestrictedProbe).Effect == Effect.Allow;

        var scopeIds = valid
            .Where(g => g.ScopeUnitId is not null)
            .Select(g => g.ScopeUnitId!.Value)
            .Distinct()
            .ToList();

        var roots = new List<Guid>();
        var denied = new List<Guid>();
        foreach (var scopeId in scopeIds)
        {
            var inScope = await FilterInScopeAsync(valid, request with { ResourceScopeUnitId = scopeId }, ct);
            if (inScope.Count == 0)
            {
                continue;
            }

            if (PickWinner(inScope).Effect == Effect.Allow)
            {
                roots.Add(scopeId);
            }
            else
            {
                denied.Add(scopeId);
            }
        }

        if (isUnrestricted)
        {
            return new AccessibleScopeResult(true, Array.Empty<Guid>(), denied);
        }

        return new AccessibleScopeResult(false, roots, denied);
    }

    private async Task<List<Grant>> FilterInScopeAsync(
        IReadOnlyList<Grant> valid,
        AccessRequest request,
        CancellationToken ct)
    {
        var inScope = new List<Grant>();
        foreach (var grant in valid)
        {
            if (!await _scopeMatcher.MatchesAsync(grant.ScopeUnitId, request.ResourceScopeUnitId, ct))
            {
                continue;
            }

            if (!GrantConstraintEvaluator.AllSatisfied(grant, request, out _))
            {
                continue;
            }

            inScope.Add(grant);
        }

        return inScope;
    }

    /// <summary>
    /// Product lock: Deny-wins at equal priority (TDD A2 left this unlocked; this codebase does not invert it).
    /// </summary>
    private static Grant PickWinner(IReadOnlyList<Grant> inScope) =>
        inScope
            .OrderByDescending(g => g.Priority)
            .ThenByDescending(g => g.Effect == Effect.Deny)
            .First();
}
