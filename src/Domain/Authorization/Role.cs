namespace qc_authorization.Domain.Authorization;

public class Role : BaseAuditableEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }

    public List<RolePermission> Permissions { get; set; } = new();
}
