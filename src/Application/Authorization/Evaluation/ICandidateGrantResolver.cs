using qc_authorization.Domain.Authorization;

namespace qc_authorization.Application.Authorization.Evaluation;

/// <summary>
/// Resolves candidate grants for a request. Phase 03 returns direct
/// grants whose <c>(SubjectType, SubjectId, Permission)</c> matches. Phase
/// 04 will extend this with position propagation. The evaluator is
/// agnostic to which resolver is plugged in.
/// </summary>
public interface ICandidateGrantResolver
{
    Task<IReadOnlyList<Grant>> ResolveAsync(AccessRequest request, CancellationToken cancellationToken);
}
