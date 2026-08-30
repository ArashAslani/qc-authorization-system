using AccessManagement.Domain.Common;
using AccessManagement.Domain.Organization.Exceptions;

namespace AccessManagement.Domain.Organization;

/// <summary>
/// Polymorphic organization tree. Company is a node with
/// <see cref="OrganizationalUnitTypes.Company"/> — there is no separate Company table.
/// </summary>
public class OrganizationalUnit : BaseAuditableEntity, IAggregateRoot
{
    private OrganizationalUnit() { }

    public Guid? ParentId { get; private set; }
    public OrganizationalUnit? Parent { get; private set; }
    public List<OrganizationalUnit> Children { get; private set; } = new();

    public string UnitType { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;

    public static OrganizationalUnit Create(string unitType, string name, Guid? parentId = null)
    {
        if (string.IsNullOrWhiteSpace(unitType))
        {
            throw new OrganizationDomainException("UnitType is required.");
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new OrganizationDomainException("Organizational unit name is required.");
        }

        return new OrganizationalUnit
        {
            UnitType = unitType.Trim(),
            Name = name.Trim(),
            ParentId = parentId,
        };
    }

    public void Reparent(OrganizationalUnit? newParent, IReadOnlyCollection<OrganizationalUnit> allUnits)
    {
        OrganizationalUnitHierarchy.EnsureValidParenting(this, newParent, allUnits);
        ParentId = newParent?.Id;
        Parent = newParent;
    }
}
