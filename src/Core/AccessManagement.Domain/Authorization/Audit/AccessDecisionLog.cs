using AccessManagement.Domain.Common;

namespace AccessManagement.Domain.Authorization.Audit;

/// <summary>
/// Answers "why was this decision made?" Distinct from
/// <see cref="AuthorizationAuditEntry"/> which records configuration changes.
/// </summary>
public class AccessDecisionLog : BaseEntity
{
    public Guid RequestedByUserId { get; set; }
    public Guid? ActivePositionId { get; set; }
    public string PermissionCode { get; set; } = string.Empty;
    public string? ResourceId { get; set; }
    public Guid? ScopeUnitId { get; set; }
    public string Decision { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public string CandidateGrantsJson { get; set; } = "[]";
    public DateTimeOffset CreatedAt { get; set; }
}
