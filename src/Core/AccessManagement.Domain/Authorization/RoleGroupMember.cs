namespace AccessManagement.Domain.Authorization;

public class RoleGroupMember : BaseAuditableEntity
{
    public Guid RoleGroupId { get; set; }
    public RoleGroup RoleGroup { get; set; } = null!;

    public Guid RoleId { get; set; }
    public Role Role { get; set; } = null!;
}
