using qc_authorization.Domain.Authorization.Evaluation;

namespace qc_authorization.Domain.Authorization.Constraints;

public interface IAuthorizationConstraint
{
    string Kind { get; }

    bool IsSatisfied(AccessRequest request, out string? rejectionReason);
}
