using qc_authorization.Domain.Authorization.Enums;

namespace qc_authorization.Application.Authorization.Evaluation;

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
/// system that carries a final Allow/Deny. See ADR 0002.
/// </summary>
public sealed record AccessDecision(
    Effect Effect,
    DecisionReason Reason,
    DecisionTrace Trace)
{
    public bool IsAllowed => Effect == Effect.Allow;
}
