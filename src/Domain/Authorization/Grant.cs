using qc_authorization.Domain.Authorization.Constraints;
using qc_authorization.Domain.Authorization.Enums;
using qc_authorization.Domain.Authorization.Exceptions;
using qc_authorization.Domain.Authorization.ValueObjects;
using qc_authorization.Domain.Common;

namespace qc_authorization.Domain.Authorization;

/// <summary>
/// A Grant is a fact. It does not decide anything; the Access Evaluation
/// Engine reads it and decides. No propagation, override, or constraint
/// evaluation lives on this type.
/// </summary>
public class Grant : BaseAuditableEntity, IAggregateRoot
{
    private Grant() { }

    public SubjectType SubjectType { get; private set; }
    public int SubjectId { get; private set; }
    public Guid? SubjectUserId { get; private set; }

    public int PermissionId { get; private set; }
    public Permission Permission { get; private set; } = null!;

    public string? Resource { get; private set; }
    public string? ResourceId { get; private set; }

    public ScopeKind ScopeKind { get; private set; } = ScopeKind.Unbounded;
    public string? ScopeIdentifier { get; private set; }

    public Effect Effect { get; private set; } = Effect.Allow;

    public SourceType SourceType { get; private set; }
    public int SourceId { get; private set; }
    public Guid? SourceUserId { get; private set; }

    public DateTimeOffset ValidFrom { get; private set; }
    public DateTimeOffset? ValidTo { get; private set; }

    public int Priority { get; private set; }

    public ICollection<GrantConstraint> Constraints { get; private set; } = new List<GrantConstraint>();

    public Scope Scope => new(ScopeKind, ScopeIdentifier);

    public Validity Validity => new(ValidFrom, ValidTo);

    public static Grant Create(
        SubjectType subjectType,
        int subjectId,
        int permissionId,
        SourceType sourceType,
        int sourceId,
        Effect effect,
        DateTimeOffset validFrom,
        DateTimeOffset? validTo,
        int priority,
        string? resource = null,
        string? resourceId = null,
        ScopeKind scopeKind = ScopeKind.Unbounded,
        string? scopeIdentifier = null,
        IEnumerable<GrantConstraint>? constraints = null,
        Guid? subjectUserId = null,
        Guid? sourceUserId = null)
    {
        _ = new Validity(validFrom, validTo);
        _ = new Scope(scopeKind, scopeIdentifier);

        if (sourceId <= 0 && sourceUserId is null)
        {
            throw new AuthorizationDomainException("SourceId or SourceUserId must be provided.");
        }

        if (subjectType == SubjectType.User && subjectUserId is null)
        {
            throw new AuthorizationDomainException("SubjectUserId is required for user grants.");
        }

        var grant = new Grant
        {
            SubjectType = subjectType,
            SubjectId = subjectId,
            SubjectUserId = subjectUserId,
            PermissionId = permissionId,
            Resource = resource,
            ResourceId = resourceId,
            ScopeKind = scopeKind,
            ScopeIdentifier = scopeIdentifier,
            Effect = effect,
            SourceType = sourceType,
            SourceId = sourceId,
            SourceUserId = sourceUserId,
            ValidFrom = validFrom,
            ValidTo = validTo,
            Priority = priority,
        };

        if (constraints is not null)
        {
            foreach (var constraint in constraints)
            {
                grant.Constraints.Add(constraint);
            }
        }

        return grant;
    }

    public static Grant CreateForUser(
        Guid subjectUserId,
        int permissionId,
        SourceType sourceType,
        int sourceId,
        Effect effect,
        DateTimeOffset validFrom,
        DateTimeOffset? validTo,
        int priority,
        Guid? sourceUserId = null,
        string? resource = null,
        string? resourceId = null,
        ScopeKind scopeKind = ScopeKind.Unbounded,
        string? scopeIdentifier = null,
        IEnumerable<GrantConstraint>? constraints = null) =>
        Create(
            SubjectType.User,
            0,
            permissionId,
            sourceType,
            sourceId,
            effect,
            validFrom,
            validTo,
            priority,
            resource,
            resourceId,
            scopeKind,
            scopeIdentifier,
            constraints,
            subjectUserId,
            sourceUserId ?? (sourceType == SourceType.User ? subjectUserId : null));
}
