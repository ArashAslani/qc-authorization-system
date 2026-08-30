using AccessManagement.Application.Abstractions;
using AccessManagement.Domain.Authorization;
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

        var inScope = new List<Grant>();
        foreach (var grant in valid)
        {
            if (await _scopeMatcher.MatchesAsync(grant.ScopeUnitId, request.ResourceScopeUnitId, ct))
            {
                inScope.Add(grant);
            }
        }

        var traceId = Guid.NewGuid();

        if (inScope.Count == 0)
        {
            var reason = candidates.Count == 0 || valid.Count == 0
                ? (candidates.Count == 0 ? AccessDecisionReasons.NoGrant : AccessDecisionReasons.Expired)
                : AccessDecisionReasons.OutOfScope;
            var denied = AccessDecision.Deny(traceId, reason);
            return await _traceWriter.WriteAsync(request, candidates, null, denied, ct);
        }

        var winner = inScope
            .OrderByDescending(g => g.Priority)
            .ThenByDescending(g => g.Effect == Effect.Deny)
            .First();

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
        var valid = candidates
            .Where(g => g.Validity.IsActiveAt(request.When) && g.Effect == Effect.Allow)
            .ToList();

        if (valid.Exists(g => g.ScopeUnitId is null))
        {
            return new AccessibleScopeResult(true, Array.Empty<Guid>());
        }

        var roots = valid
            .Select(g => g.ScopeUnitId!.Value)
            .Distinct()
            .ToList();

        return new AccessibleScopeResult(false, roots);
    }
}
