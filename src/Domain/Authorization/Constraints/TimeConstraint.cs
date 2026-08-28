using qc_authorization.Domain.Authorization.Evaluation;
using qc_authorization.Domain.Authorization.Exceptions;

namespace qc_authorization.Domain.Authorization.Constraints;

public sealed class TimeConstraint : IAuthorizationConstraint
{
    public string Kind => nameof(TimeConstraint);

    public TimeOnly From { get; }
    public TimeOnly To { get; }

    public TimeConstraint(TimeOnly from, TimeOnly to)
    {
        if (from >= to)
        {
            throw new AuthorizationDomainException("Time constraint window must have From before To.");
        }

        From = from;
        To = to;
    }

    public bool IsSatisfied(AccessRequest request, out string? rejectionReason)
    {
        var time = TimeOnly.FromDateTime(request.When.UtcDateTime);
        if (time < From || time >= To)
        {
            rejectionReason = "outside-time-window";
            return false;
        }

        rejectionReason = null;
        return true;
    }
}
