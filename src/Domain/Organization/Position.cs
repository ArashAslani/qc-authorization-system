namespace qc_authorization.Domain.Organization;

public class Position : BaseAuditableEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;

    public int? ParentId { get; set; }
    public Position? Parent { get; set; }
    public List<Position> Children { get; set; } = new();
}
