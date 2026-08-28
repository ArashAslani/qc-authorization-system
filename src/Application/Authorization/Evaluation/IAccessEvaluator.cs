namespace qc_authorization.Application.Authorization.Evaluation;

/// <summary>
/// The single owner of Allow/Deny. See ADR 0002.
/// </summary>
public interface IAccessEvaluator
{
    Task<AccessDecision> EvaluateAsync(AccessRequest request, CancellationToken cancellationToken = default);
}
