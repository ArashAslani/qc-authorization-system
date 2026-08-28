namespace qc_authorization.Domain.Authorization;

public class Permission : BaseAuditableEntity
{
    /// <summary>
    /// Stable business code, e.g. <c>PERSONNEL.UPDATE</c>.
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Logical resource the permission operates on, e.g. <c>Personnel</c>.
    /// </summary>
    public string Resource { get; set; } = string.Empty;

    /// <summary>
    /// Action the permission allows, e.g. <c>Read</c>, <c>Update</c>.
    /// </summary>
    public string Action { get; set; } = string.Empty;

    public string? Description { get; set; }
}
