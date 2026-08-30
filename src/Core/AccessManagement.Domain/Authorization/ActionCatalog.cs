using AccessManagement.Domain.Authorization.Exceptions;

namespace AccessManagement.Domain.Authorization;

public class ActionCatalog : BaseAuditableEntity, IAggregateRoot
{
    private ActionCatalog() { }

    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;

    public static ActionCatalog Create(string code, string name)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            throw new AuthorizationDomainException("Action code is required.");
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new AuthorizationDomainException("Action name is required.");
        }

        return new ActionCatalog
        {
            Code = code.Trim().ToUpperInvariant(),
            Name = name.Trim(),
        };
    }
}
