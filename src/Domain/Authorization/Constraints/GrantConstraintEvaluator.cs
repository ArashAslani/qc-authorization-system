using qc_authorization.Domain.Authorization;
using qc_authorization.Domain.Authorization.Evaluation;

namespace qc_authorization.Domain.Authorization.Constraints;

public static class GrantConstraintEvaluator
{
    public static bool AllSatisfied(Grant grant, AccessRequest request, out string? rejectionReason)
    {
        foreach (var constraint in grant.Constraints)
        {
            if (!constraint.IsSatisfied(request, out rejectionReason))
            {
                rejectionReason = $"{constraint.Kind}:{rejectionReason}";
                return false;
            }
        }

        rejectionReason = null;
        return true;
    }
}
