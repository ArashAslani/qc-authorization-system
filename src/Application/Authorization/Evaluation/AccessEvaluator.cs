using qc_authorization.Domain.Authorization;
using qc_authorization.Domain.Authorization.Enums;
using qc_authorization.Domain.Authorization.ValueObjects;

namespace qc_authorization.Application.Authorization.Evaluation;

/// <summary>
/// The Access Evaluation Engine. The only type in the system that
/// returns a final <see cref="AccessDecision"/>. Pipeline:
///   Candidate Grants -> Validity -> Scope -> Priority -> Effect -> Decision -> Trace.
/// </summary>
public class AccessEvaluator : IAccessEvaluator
{
    private readonly ICandidateGrantResolver _candidateResolver;

    public AccessEvaluator(ICandidateGrantResolver candidateResolver) =>
        _candidateResolver = candidateResolver;

    public async Task<AccessDecision> EvaluateAsync(AccessRequest request, CancellationToken cancellationToken = default)
    {
        var traceId = Guid.NewGuid().ToString("N");
        var candidates = await _candidateResolver.ResolveAsync(request, cancellationToken);

        if (candidates.Count == 0)
        {
            var trace = new DecisionTrace(
                traceId,
                request.SubjectType,
                request.SubjectId,
                request.PermissionCode,
                request.Resource,
                request.ResourceId,
                Array.Empty<Grant>(),
                Array.Empty<Grant>(),
                Array.Empty<RejectedGrant>(),
                Array.Empty<ConflictResolutionEntry>(),
                ScopeResult: false,
                ValidityResult: false,
                FinalDecision: Effect.Deny,
                Reason: "no-candidate-grants");
            return new AccessDecision(Effect.Deny, DecisionReason.NoCandidateGrants, trace);
        }

        // Validity filter.
        var applicable = new List<Grant>();
        var rejected = new List<RejectedGrant>();
        foreach (var g in candidates)
        {
            if (g.ValidFrom > request.When)
            {
                rejected.Add(RejectedGrant.For(g, "not-yet-valid"));
                continue;
            }
            if (g.ValidTo is { } end && request.When > end)
            {
                rejected.Add(RejectedGrant.For(g, "expired"));
                continue;
            }
            applicable.Add(g);
        }

        if (applicable.Count == 0)
        {
            var trace = new DecisionTrace(
                traceId,
                request.SubjectType,
                request.SubjectId,
                request.PermissionCode,
                request.Resource,
                request.ResourceId,
                candidates,
                Array.Empty<Grant>(),
                rejected,
                Array.Empty<ConflictResolutionEntry>(),
                ScopeResult: false,
                ValidityResult: false,
                FinalDecision: Effect.Deny,
                Reason: "all-grants-out-of-validity");
            return new AccessDecision(Effect.Deny, DecisionReason.Expired, trace);
        }

        // Scope filter.
        var inScope = new List<Grant>();
        foreach (var g in applicable)
        {
            if (ScopeMatches(g, request))
            {
                inScope.Add(g);
            }
            else
            {
                rejected.Add(RejectedGrant.For(g, "out-of-scope"));
            }
        }

        if (inScope.Count == 0)
        {
            var trace = new DecisionTrace(
                traceId,
                request.SubjectType,
                request.SubjectId,
                request.PermissionCode,
                request.Resource,
                request.ResourceId,
                candidates,
                Array.Empty<Grant>(),
                rejected,
                Array.Empty<ConflictResolutionEntry>(),
                ScopeResult: false,
                ValidityResult: true,
                FinalDecision: Effect.Deny,
                Reason: "out-of-scope");
            return new AccessDecision(Effect.Deny, DecisionReason.OutOfScope, trace);
        }

        // Conflict resolution: highest Priority wins. Tie-break: Deny > Allow.
        var ordered = inScope
            .OrderByDescending(g => g.Priority)
            .ThenByDescending(g => g.Effect) // Effect.Deny (1) > Effect.Allow (0)
            .ToList();

        var winner = ordered[0];
        var conflict = ordered
            .Select((g, idx) => new ConflictResolutionEntry(g.Id, g.SourceType, g.SourceId, g.Priority, g.Effect, Won: idx == 0))
            .ToList();

        var decision = winner.Effect;
        var (reason, _) = decision == Effect.Allow
            ? (DecisionReason.Allowed, "allowed-by-priority")
            : (DecisionReason.Denied, "denied-by-priority");

        if (inScope.Count > 1)
        {
            reason = decision == Effect.Allow
                ? DecisionReason.ConflictResolvedByPriority
                : DecisionReason.ConflictResolvedByDenyOverAllow;
        }

        var finalTrace = new DecisionTrace(
            traceId,
            request.SubjectType,
            request.SubjectId,
            request.PermissionCode,
            request.Resource,
            request.ResourceId,
            candidates,
            inScope,
            rejected,
            conflict,
            ScopeResult: true,
            ValidityResult: true,
            FinalDecision: decision,
            Reason: reason.ToString());

        return new AccessDecision(decision, reason, finalTrace);
    }

    private static bool ScopeMatches(Grant g, AccessRequest request)
    {
        // Unbounded grant: always matches.
        if (g.ScopeKind == ScopeKind.Unbounded) return true;

        // Bounded grant: requires the request to identify the same scope
        // either explicitly (via ResourceId) or via Context["Scope"].
        var scopeFromContext = request.Context is not null
            && request.Context.TryGetValue("Scope", out var v) ? v as string : null;
        var requestedScope = request.ResourceId ?? scopeFromContext;

        return requestedScope is not null
            && string.Equals(requestedScope, g.ScopeIdentifier, StringComparison.Ordinal);
    }
}
