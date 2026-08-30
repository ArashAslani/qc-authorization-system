using AccessManagement.Domain.Common;
using AccessManagement.Domain.Organization.Enums;
using AccessManagement.Domain.Organization.Exceptions;

namespace AccessManagement.Domain.Organization;

public class Position : BaseAuditableEntity, IAggregateRoot
{
    private Position() { }

    /// <summary>
    /// FK to <see cref="OrganizationalUnit"/> whose UnitType is Company.
    /// </summary>
    public Guid CompanyUnitId { get; private set; }
    public OrganizationalUnit? CompanyUnit { get; private set; }

    public string Code { get; private set; } = string.Empty;
    public string Title { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public PositionStatus Status { get; private set; } = PositionStatus.Active;

    public Guid? ParentPositionId { get; private set; }
    public Position? Parent { get; private set; }
    public List<Position> Children { get; private set; } = new();

    public static Position Create(
        Guid companyUnitId,
        string code,
        string title,
        string? description = null,
        Guid? parentPositionId = null,
        PositionStatus status = PositionStatus.Active)
    {
        if (companyUnitId == Guid.Empty)
        {
            throw new OrganizationDomainException("CompanyUnitId must be a valid identifier.");
        }

        if (string.IsNullOrWhiteSpace(code))
        {
            throw new OrganizationDomainException("Position code is required.");
        }

        if (string.IsNullOrWhiteSpace(title))
        {
            throw new OrganizationDomainException("Position title is required.");
        }

        return new Position
        {
            CompanyUnitId = companyUnitId,
            Code = code.Trim(),
            Title = title.Trim(),
            Description = description?.Trim(),
            ParentPositionId = parentPositionId,
            Status = status,
        };
    }

    public void Reparent(Position? newParent, IReadOnlyCollection<Position> allPositions, PositionHierarchyService hierarchy)
    {
        hierarchy.EnsureValidParenting(this, newParent, allPositions);
        ParentPositionId = newParent?.Id;
        Parent = newParent;
    }

    public void Update(string title, string? description, PositionStatus status)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new OrganizationDomainException("Position title is required.");
        }

        Title = title.Trim();
        Description = description?.Trim();
        Status = status;
    }
}
