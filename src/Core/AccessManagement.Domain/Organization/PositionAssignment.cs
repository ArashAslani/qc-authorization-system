using AccessManagement.Domain.Common;
using AccessManagement.Domain.Organization.Exceptions;

namespace AccessManagement.Domain.Organization;

public class PositionAssignment : BaseAuditableEntity, IAggregateRoot
{
    private PositionAssignment() { }

    public Guid PersonnelId { get; private set; }
    public Personnel Personnel { get; private set; } = null!;

    public Guid PositionId { get; private set; }
    public Position Position { get; private set; } = null!;

    public DateTimeOffset ValidFrom { get; private set; }
    public DateTimeOffset? ValidTo { get; private set; }

    /// <summary>
    /// When true, this assignment determines the default company workspace at login.
    /// At most one assignment per personnel may be primary (enforced in application layer).
    /// </summary>
    public bool IsPrimary { get; private set; }

    public static PositionAssignment Create(
        Guid personnelId,
        Guid positionId,
        DateTimeOffset validFrom,
        DateTimeOffset? validTo = null)
    {
        if (personnelId == Guid.Empty)
        {
            throw new OrganizationDomainException("PersonnelId must be a positive identifier.");
        }

        if (positionId == Guid.Empty)
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

    public void MarkAsPrimary() => IsPrimary = true;

    public void ClearPrimary() => IsPrimary = false;

    public void End(DateTimeOffset atUtc)
    {
        if (ValidTo is { } existing && existing <= atUtc)
        {
            return;
        }

        ValidTo = atUtc;
    }
}
