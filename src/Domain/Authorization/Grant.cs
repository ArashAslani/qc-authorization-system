using qc_authorization.Domain.Authorization.Enums;
using qc_authorization.Domain.Authorization.ValueObjects;

namespace qc_authorization.Domain.Authorization;

/// <summary>
/// A Grant is a fact. It does not decide anything; the Access Evaluation
/// Engine reads it and decides. No business logic, no propagation, no
/// override resolution, no constraint evaluation lives on this type.
/// </summary>
public class Grant : BaseAuditableEntity
{
    public SubjectType SubjectType { get; set; }
    public int SubjectId { get; set; }

    public int PermissionId { get; set; }
    public Permission Permission { get; set; } = null!;

    public string? Resource { get; set; }
    public string? ResourceId { get; set; }

    public ScopeKind ScopeKind { get; set; } = ScopeKind.Unbounded;
    public string? ScopeIdentifier { get; set; }

    public Effect Effect { get; set; } = Effect.Allow;

    public SourceType SourceType { get; set; }
    public int SourceId { get; set; }

    public DateTimeOffset ValidFrom { get; set; }
    public DateTimeOffset? ValidTo { get; set; }

    public int Priority { get; set; }

    public Scope Scope => new(ScopeKind, ScopeIdentifier);

    public Validity Validity => new(ValidFrom, ValidTo);
}
