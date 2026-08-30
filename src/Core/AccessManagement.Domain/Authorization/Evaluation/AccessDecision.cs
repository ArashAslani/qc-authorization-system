using AccessManagement.Domain.Authorization.Enums;

namespace AccessManagement.Domain.Authorization.Evaluation;

public static class AccessDecisionReasons
{
    public const string NoGrant = "NO_GRANT";
    public const string Expired = "EXPIRED";
    public const string OutOfScope = "OUT_OF_SCOPE";
    public const string Overridden = "OVERRIDDEN";
    public const string Allowed = "ALLOWED";
}

public enum DecisionReason
{
    NoCandidateGrants = 0,
    OutOfScope = 1,
    Expired = 2,
    NotYetValid = 3,
    ConflictResolvedByPriority = 4,
    ConflictResolvedByDenyOverAllow = 5,
    Allowed = 6,
    Denied = 7,
}

/// <summary>
/// Output of the Access Evaluation Engine. This is the only type in the
/// system that carries a final Allow/Deny.
/// </summary>
public sealed record AccessDecision(
    bool Allowed,
    string Reason,
    Guid TraceId)
{
    public DecisionTrace? Trace { get; init; }

    public bool IsAllowed => Allowed;

    public Effect Effect => Allowed ? Effect.Allow : Effect.Deny;

    public static AccessDecision Allow(Guid traceId, string reason = AccessDecisionReasons.Allowed, DecisionTrace? trace = null) =>
        new(true, reason, traceId) { Trace = trace };

    public static AccessDecision Deny(Guid traceId, string reason, DecisionTrace? trace = null) =>
        new(false, reason, traceId) { Trace = trace };
}
