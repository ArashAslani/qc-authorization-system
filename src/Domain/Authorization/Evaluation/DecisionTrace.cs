using qc_authorization.Domain.Authorization.Enums;

namespace qc_authorization.Domain.Authorization.Evaluation;

/// <summary>
/// Mandatory per-decision trace (see ADR 0007).
/// </summary>
public sealed record DecisionTrace(
    string TraceId,
    SubjectType Subject,
    Guid SubjectId,
    string RequestedPermission,
    string Resource,
    string? ResourceId,
    IReadOnlyList<Grant> CandidateGrants,
    IReadOnlyList<Grant> ApplicableGrants,
    IReadOnlyList<RejectedGrant> RejectedGrants,
    IReadOnlyList<ConflictResolutionEntry> ConflictResolution,
    bool ScopeResult,
    bool ValidityResult,
    Effect FinalDecision,
    string Reason)
{
    public static DecisionTrace Empty() => new(
        Guid.NewGuid().ToString("N"),
        SubjectType.User,
        Guid.Empty,
        string.Empty,
        string.Empty,
        null,
        Array.Empty<Grant>(),
        Array.Empty<Grant>(),
        Array.Empty<RejectedGrant>(),
        Array.Empty<ConflictResolutionEntry>(),
        ScopeResult: true,
        ValidityResult: true,
        FinalDecision: Effect.Allow,
        Reason: "no-candidates");
}

public sealed record RejectedGrant(
    Guid GrantId,
    SourceType SourceType,
    Guid SourceId,
    string Reason)
{
    public static RejectedGrant For(Grant g, string reason) =>
        new(g.Id, g.SourceType, g.SourceId, reason);
}

public sealed record ConflictResolutionEntry(
    Guid GrantId,
    SourceType SourceType,
    Guid SourceId,
    int Priority,
    Effect Effect,
    bool Won);
