using AccessManagement.Domain.Authorization;
using AccessManagement.Domain.Authorization.Evaluation;

namespace AccessManagement.Application.Authorization.Evaluation;

public interface ICandidateGrantResolver
{
    Task<IReadOnlyList<Grant>> ResolveAsync(AccessRequest request, CancellationToken cancellationToken);
}
