namespace qc_authorization.Domain.Organization;

public class PositionAssignment : BaseAuditableEntity
{
    public int PersonnelId { get; set; }
    public Personnel Personnel { get; set; } = null!;

    public int PositionId { get; set; }
    public Position Position { get; set; } = null!;

    public DateTimeOffset ValidFrom { get; set; }
    public DateTimeOffset? ValidTo { get; set; }
}
