using AccessManagement.Domain.Authorization.Enums;
using AccessManagement.Domain.Authorization.Exceptions;
using AccessManagement.Domain.Common;

namespace AccessManagement.Domain.Authorization;

public class RoleGroup : BaseAuditableEntity, IAggregateRoot
{
    private RoleGroup() { }

    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public CatalogStatus Status { get; private set; } = CatalogStatus.Active;

    public List<RoleGroupMember> Members { get; private set; } = new();

    public static RoleGroup Create(string code, string name, string? description = null)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            throw new AuthorizationDomainException("RoleGroup code is required.");
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new AuthorizationDomainException("RoleGroup name is required.");
        }

        return new RoleGroup
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
            throw new AuthorizationDomainException("RoleGroup name is required.");
        }

        Name = name.Trim();
        Description = description?.Trim();
        Status = status;
    }

    public void Activate() => Status = CatalogStatus.Active;

    public void Deactivate() => Status = CatalogStatus.Inactive;

    public void AddRole(Role role)
    {
        if (Members.Any(m => m.RoleId == role.Id))
        {
            throw new AuthorizationDomainException($"Role {role.Code} is already in group {Code}.");
        }

        Members.Add(new RoleGroupMember { RoleGroup = this, RoleGroupId = Id, Role = role, RoleId = role.Id });
    }
}
