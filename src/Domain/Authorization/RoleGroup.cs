using qc_authorization.Domain.Authorization.Exceptions;
using qc_authorization.Domain.Common;

namespace qc_authorization.Domain.Authorization;

public class RoleGroup : BaseAuditableEntity, IAggregateRoot
{
    private RoleGroup() { }

    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }

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

    public void AddRole(Role role)
    {
        if (Members.Any(m => m.RoleId == role.Id))
        {
            throw new AuthorizationDomainException($"Role {role.Code} is already in group {Code}.");
        }

        Members.Add(new RoleGroupMember { RoleGroup = this, Role = role, RoleId = role.Id });
    }
}
