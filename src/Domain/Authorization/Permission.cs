using qc_authorization.Domain.Authorization.Exceptions;
using qc_authorization.Domain.Common;

namespace qc_authorization.Domain.Authorization;

public class Permission : BaseAuditableEntity, IAggregateRoot
{
    private Permission() { }

    public string Code { get; private set; } = string.Empty;
    public string Resource { get; private set; } = string.Empty;
    public string Action { get; private set; } = string.Empty;
    public string? Description { get; private set; }

    public static Permission Create(string code, string resource, string action, string? description = null)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            throw new AuthorizationDomainException("Permission code is required.");
        }

        if (string.IsNullOrWhiteSpace(resource))
        {
            throw new AuthorizationDomainException("Permission resource is required.");
        }

        if (string.IsNullOrWhiteSpace(action))
        {
            throw new AuthorizationDomainException("Permission action is required.");
        }

        return new Permission
        {
            Code = code.Trim().ToUpperInvariant(),
            Resource = resource.Trim(),
            Action = action.Trim(),
            Description = description,
        };
    }
}
