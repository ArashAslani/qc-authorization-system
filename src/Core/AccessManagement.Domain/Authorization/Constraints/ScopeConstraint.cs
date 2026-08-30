using AccessManagement.Domain.Authorization.Evaluation;
using AccessManagement.Domain.Authorization.Exceptions;

namespace AccessManagement.Domain.Authorization.Constraints;

public sealed class ScopeConstraint : IAuthorizationConstraint
{
    public string Kind => nameof(ScopeConstraint);

    public string ScopeKey { get; }
    public string ScopeValue { get; }

    public ScopeConstraint(string scopeKey, string scopeValue)
    {
        if (string.IsNullOrWhiteSpace(scopeKey) || string.IsNullOrWhiteSpace(scopeValue))
        {
            throw new AuthorizationDomainException("Scope constraint key and value are required.");
        }

        ScopeKey = scopeKey.Trim();
        ScopeValue = scopeValue.Trim();
    }

    public bool IsSatisfied(AccessRequest request, out string? rejectionReason)
    {
        if (request.Context is null || !request.Context.TryGetValue(ScopeKey, out var raw))
        {
            rejectionReason = "scope-key-missing";
            return false;
        }

        if (!string.Equals(Convert.ToString(raw), ScopeValue, StringComparison.Ordinal))
        {
            rejectionReason = "scope-value-mismatch";
            return false;
        }

        rejectionReason = null;
        return true;
    }
}
