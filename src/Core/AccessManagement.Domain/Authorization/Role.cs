using AccessManagement.Domain.Authorization.Enums;
using AccessManagement.Domain.Authorization.Exceptions;
using AccessManagement.Domain.Common;

namespace AccessManagement.Domain.Authorization;

public class Role : BaseAuditableEntity, IAggregateRoot
{
    private Role() { }

    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public CatalogStatus Status { get; private set; } = CatalogStatus.Active;

    /// <summary>
    /// Optional parent in the role catalog. Flattened at materialize time (not by the engine).
    /// Set only in <see cref="Create"/>; there is no public re-parent method.
    /// If re-parenting is added later, the same command must rematerialize
    /// (call <c>RoleGrantRematerializer</c>) so assignment grants do not go stale.
    /// </summary>
    public Guid? ParentRoleId { get; private set; }

    public List<RolePermission> Permissions { get; private set; } = new();

    public static Role Create(string code, string name, string? description = null, Guid? parentRoleId = null)
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
            ParentRoleId = parentRoleId,
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
