using AccessManagement.Domain.Authorization.Exceptions;
using AccessManagement.Domain.Common;

namespace AccessManagement.Domain.Authorization;

public class ResourceCatalog : BaseAuditableEntity, IAggregateRoot
{
    private ResourceCatalog() { }

    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }

    public static ResourceCatalog Create(string code, string name, string? description = null)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            throw new AuthorizationDomainException("Resource code is required.");
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new AuthorizationDomainException("Resource name is required.");
        }

        return new ResourceCatalog
        {
            Code = code.Trim().ToUpperInvariant(),
            Name = name.Trim(),
            Description = description,
        };
    }
}
