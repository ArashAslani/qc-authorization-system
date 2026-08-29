using qc_authorization.Domain.Common;
using qc_authorization.Domain.Organization.Enums;
using qc_authorization.Domain.Organization.Exceptions;

namespace qc_authorization.Domain.Organization;

public class Position : BaseAuditableEntity, IAggregateRoot
{
    private Position() { }

    public Guid CompanyId { get; private set; }
    public string Code { get; private set; } = string.Empty;
    public string Title { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public PositionStatus Status { get; private set; } = PositionStatus.Active;

    public Guid? ParentPositionId { get; private set; }
    public Position? Parent { get; private set; }
    public List<Position> Children { get; private set; } = new();

    public static Position Create(
        Guid companyId,
        string code,
        string title,
        string? description = null,
        Guid? parentPositionId = null,
        PositionStatus status = PositionStatus.Active)
    {
        if (companyId == Guid.Empty)
        {
            throw new OrganizationDomainException("CompanyId must be a valid identifier.");
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
            CompanyId = companyId,
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
}
