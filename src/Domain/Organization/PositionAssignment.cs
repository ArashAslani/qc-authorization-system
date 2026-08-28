using qc_authorization.Domain.Common;
using qc_authorization.Domain.Organization.Exceptions;

namespace qc_authorization.Domain.Organization;

public class PositionAssignment : BaseAuditableEntity, IAggregateRoot
{
    private PositionAssignment() { }

    public int PersonnelId { get; private set; }
    public Personnel Personnel { get; private set; } = null!;

    public int PositionId { get; private set; }
    public Position Position { get; private set; } = null!;

    public DateTimeOffset ValidFrom { get; private set; }
    public DateTimeOffset? ValidTo { get; private set; }

    public static PositionAssignment Create(
        int personnelId,
        int positionId,
        DateTimeOffset validFrom,
        DateTimeOffset? validTo = null)
    {
        if (personnelId <= 0)
        {
            throw new OrganizationDomainException("PersonnelId must be a positive identifier.");
        }

        if (positionId <= 0)
        {
            throw new OrganizationDomainException("PositionId must be a positive identifier.");
        }

        if (validTo is { } end && end < validFrom)
        {
            throw new OrganizationDomainException("ValidTo cannot be earlier than ValidFrom.");
        }

        return new PositionAssignment
        {
            PersonnelId = personnelId,
            PositionId = positionId,
            ValidFrom = validFrom,
            ValidTo = validTo,
        };
    }
}
