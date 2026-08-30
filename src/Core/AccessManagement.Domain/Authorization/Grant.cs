using AccessManagement.Domain.Authorization.Constraints;
using AccessManagement.Domain.Authorization.Enums;
using AccessManagement.Domain.Authorization.Exceptions;
using AccessManagement.Domain.Authorization.ValueObjects;
using AccessManagement.Domain.Common;

namespace AccessManagement.Domain.Authorization;

/// <summary>
/// A Grant is a fact. It does not decide anything; the Access Evaluation
/// Engine reads it and decides. No propagation, override, or constraint
/// evaluation lives on this type.
/// </summary>
public class Grant : BaseAuditableEntity, IAggregateRoot
{
    private Grant() { }

    public SubjectType SubjectType { get; private set; }
    public Guid SubjectId { get; private set; }
    public Guid? SubjectUserId { get; private set; }

    public Guid PermissionId { get; private set; }
    public Permission Permission { get; private set; } = null!;

    public string? Resource { get; private set; }
    public string? ResourceId { get; private set; }

    /// <summary>
    /// Null means unrestricted scope (User Admin / super-admin only at write time).
    /// </summary>
    public Guid? ScopeUnitId { get; private set; }

    public Effect Effect { get; private set; } = Effect.Allow;

    public SourceType SourceType { get; private set; }
    public Guid SourceId { get; private set; }
    public Guid? SourceUserId { get; private set; }

    public DateTimeOffset ValidFrom { get; private set; }
    public DateTimeOffset? ValidTo { get; private set; }

    public int Priority { get; private set; }

    public ICollection<GrantConstraint> Constraints { get; private set; } = new List<GrantConstraint>();

    public Validity Validity => new(ValidFrom, ValidTo);

    public static Grant Create(
        SubjectType subjectType,
        Guid subjectId,
        Guid permissionId,
        SourceType sourceType,
        Guid sourceId,
        Effect effect,
        DateTimeOffset validFrom,
        DateTimeOffset? validTo,
        int priority,
        string? resource = null,
        string? resourceId = null,
        Guid? scopeUnitId = null,
        IEnumerable<GrantConstraint>? constraints = null,
        Guid? subjectUserId = null,
        Guid? sourceUserId = null)
    {
        _ = new Validity(validFrom, validTo);

        if (sourceId == Guid.Empty && sourceUserId is null)
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
            ScopeUnitId = scopeUnitId,
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
        Guid permissionId,
        SourceType sourceType,
        Guid sourceId,
        Effect effect,
        DateTimeOffset validFrom,
        DateTimeOffset? validTo,
        int priority,
        Guid? sourceUserId = null,
        string? resource = null,
        string? resourceId = null,
        Guid? scopeUnitId = null,
        IEnumerable<GrantConstraint>? constraints = null) =>
        Create(
            SubjectType.User,
            Guid.Empty,
            permissionId,
            sourceType,
            sourceId,
            effect,
            validFrom,
            validTo,
            priority,
            resource,
            resourceId,
            scopeUnitId,
            constraints,
            subjectUserId,
            sourceUserId ?? (sourceType == SourceType.User ? subjectUserId : null));

    /// <summary>
    /// Soft-deactivate. The row is never deleted (A1.3).
    /// </summary>
    public void Deactivate(DateTimeOffset atUtc) => ValidTo = atUtc;
}
