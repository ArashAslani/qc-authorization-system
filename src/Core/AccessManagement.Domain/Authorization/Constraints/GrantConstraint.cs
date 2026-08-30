using AccessManagement.Domain.Authorization.Evaluation;
using AccessManagement.Domain.Authorization.Exceptions;

namespace AccessManagement.Domain.Authorization.Constraints;

/// <summary>
/// Persisted constraint attached to a grant. Maps to typed constraint evaluators.
/// </summary>
public class GrantConstraint
{
    private GrantConstraint() { }

    public ConstraintKind Kind { get; private set; }
    public decimal? MaxAmount { get; private set; }
    public TimeOnly? TimeFrom { get; private set; }
    public TimeOnly? TimeTo { get; private set; }
    public string? ScopeKey { get; private set; }
    public string? ScopeValue { get; private set; }

    public static GrantConstraint FromAmount(decimal maxAmount) =>
        new()
        {
            Kind = ConstraintKind.Amount,
            MaxAmount = maxAmount,
        };

    public static GrantConstraint FromTime(TimeOnly from, TimeOnly to) =>
        new()
        {
            Kind = ConstraintKind.Time,
            TimeFrom = from,
            TimeTo = to,
        };

    public static GrantConstraint FromScope(string key, string value) =>
        new()
        {
            Kind = ConstraintKind.Scope,
            ScopeKey = key,
            ScopeValue = value,
        };

    public bool IsSatisfied(AccessRequest request, out string? rejectionReason)
    {
        IAuthorizationConstraint constraint = Kind switch
        {
            ConstraintKind.Amount => new AmountConstraint(MaxAmount ?? throw new AuthorizationDomainException("Amount constraint is missing MaxAmount.")),
            ConstraintKind.Time => new TimeConstraint(
                TimeFrom ?? throw new AuthorizationDomainException("Time constraint is missing TimeFrom."),
                TimeTo ?? throw new AuthorizationDomainException("Time constraint is missing TimeTo.")),
            ConstraintKind.Scope => new ScopeConstraint(
                ScopeKey ?? throw new AuthorizationDomainException("Scope constraint is missing ScopeKey."),
                ScopeValue ?? throw new AuthorizationDomainException("Scope constraint is missing ScopeValue.")),
            _ => throw new AuthorizationDomainException($"Unknown constraint kind {Kind}."),
        };

        return constraint.IsSatisfied(request, out rejectionReason);
    }
}
