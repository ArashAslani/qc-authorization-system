namespace qc_authorization.Domain.Authorization;

public class RoleGroupMember : BaseAuditableEntity
{
    public int RoleGroupId { get; set; }
    public RoleGroup RoleGroup { get; set; } = null!;

    public int RoleId { get; set; }
    public Role Role { get; set; } = null!;
}
