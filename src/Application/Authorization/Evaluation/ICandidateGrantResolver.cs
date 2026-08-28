using qc_authorization.Domain.Authorization;
using qc_authorization.Domain.Authorization.Evaluation;

namespace qc_authorization.Application.Authorization.Evaluation;

public interface ICandidateGrantResolver
{
    Task<IReadOnlyList<Grant>> ResolveAsync(AccessRequest request, CancellationToken cancellationToken);
}
