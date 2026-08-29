using qc_authorization.Domain.Authorization.Enums;
using qc_authorization.Domain.Authorization.Exceptions;
using qc_authorization.Domain.Common;

namespace qc_authorization.Domain.Authorization;

public class Role : BaseAuditableEntity, IAggregateRoot
{
    private Role() { }

    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public CatalogStatus Status { get; private set; } = CatalogStatus.Active;

    public List<RolePermission> Permissions { get; private set; } = new();

    public static Role Create(string code, string name, string? description = null)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            throw new AuthorizationDomainException("Role code is required.");
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new AuthorizationDomainException("Role name is required.");
        }

        return new Role
        {
            Code = code.Trim().ToUpperInvariant(),
            Name = name.Trim(),
            Description = description,
        };
    }

    public void Update(string name, string? description, CatalogStatus status)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new AuthorizationDomainException("Role name is required.");
        }

        Name = name.Trim();
        Description = description?.Trim();
        Status = status;
    }

    public void Activate() => Status = CatalogStatus.Active;

    public void Deactivate() => Status = CatalogStatus.Inactive;
}
