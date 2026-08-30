using AccessManagement.Domain.Authorization.Evaluation;
using AccessManagement.Domain.Authorization.Exceptions;

namespace AccessManagement.Domain.Authorization.Constraints;

public sealed class AmountConstraint : IAuthorizationConstraint
{
    public string Kind => nameof(AmountConstraint);

    public decimal MaxAmount { get; }

    public AmountConstraint(decimal maxAmount)
    {
        if (maxAmount <= 0)
        {
            throw new AuthorizationDomainException("Amount constraint max must be positive.");
        }

        MaxAmount = maxAmount;
    }

    public bool IsSatisfied(AccessRequest request, out string? rejectionReason)
    {
        if (request.Context is null || !request.Context.TryGetValue("Amount", out var raw))
        {
            rejectionReason = "amount-missing";
            return false;
        }

        var amount = Convert.ToDecimal(raw);
        if (amount > MaxAmount)
        {
            rejectionReason = "amount-exceeds-max";
            return false;
        }

        rejectionReason = null;
        return true;
    }
}
