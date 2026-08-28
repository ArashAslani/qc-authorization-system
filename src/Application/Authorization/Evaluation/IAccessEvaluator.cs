using qc_authorization.Domain.Authorization.Evaluation;

namespace qc_authorization.Application.Authorization.Evaluation;

public interface IAccessEvaluator
{
    Task<AccessDecision> EvaluateAsync(AccessRequest request, CancellationToken cancellationToken = default);
}
