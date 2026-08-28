namespace qc_authorization.Domain.Organization;

public class Personnel : BaseAuditableEntity
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? Email { get; set; }

    public List<PositionAssignment> Assignments { get; set; } = new();
}
