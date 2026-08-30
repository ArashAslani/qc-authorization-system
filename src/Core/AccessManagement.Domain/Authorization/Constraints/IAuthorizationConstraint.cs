using AccessManagement.Domain.Authorization.Evaluation;

namespace AccessManagement.Domain.Authorization.Constraints;

public interface IAuthorizationConstraint
{
    string Kind { get; }

    bool IsSatisfied(AccessRequest request, out string? rejectionReason);
}
