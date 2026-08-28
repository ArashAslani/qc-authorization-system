using qc_authorization.Domain.Authorization.Evaluation;

namespace qc_authorization.Application.Authorization.Evaluation;

/// <summary>
/// Application façade over the domain Access Evaluation Engine.
/// </summary>
public sealed class AccessEvaluator : IAccessEvaluator
{
    private readonly ICandidateGrantResolver _candidateResolver;
    private readonly AccessEvaluationEngine _engine;

    public AccessEvaluator(ICandidateGrantResolver candidateResolver, AccessEvaluationEngine engine)
    {
        _candidateResolver = candidateResolver;
        _engine = engine;
    }

    public async Task<AccessDecision> EvaluateAsync(AccessRequest request, CancellationToken cancellationToken = default)
    {
        var candidates = await _candidateResolver.ResolveAsync(request, cancellationToken);
        return _engine.Evaluate(request, candidates);
    }
}
