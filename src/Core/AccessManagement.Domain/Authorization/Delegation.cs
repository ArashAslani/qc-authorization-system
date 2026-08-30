using AccessManagement.Domain.Authorization.Enums;
using AccessManagement.Domain.Authorization.Exceptions;
using AccessManagement.Domain.Authorization.ValueObjects;
using AccessManagement.Domain.Common;

namespace AccessManagement.Domain.Authorization;

/// <summary>
/// Delegation produces a <see cref="Grant"/> with <c>SourceType = Delegation</c>.
/// It never decides Allow/Deny; the Access Evaluation Engine does.
/// </summary>
public class Delegation : BaseAuditableEntity, IAggregateRoot
{
    private Delegation() { }

    public Guid DelegatorUserId { get; private set; }
    public Guid DelegateUserId { get; private set; }

    public Guid PermissionId { get; private set; }
    public Permission Permission { get; private set; } = null!;

    public Guid? ScopeUnitId { get; private set; }

    public DateTimeOffset ValidFrom { get; private set; }
    public DateTimeOffset? ValidTo { get; private set; }

    public bool Delegable { get; private set; } = true;

    public bool IsRevoked { get; private set; }
    public DateTimeOffset? RevokedAt { get; private set; }

    public Validity Validity => new(ValidFrom, ValidTo);

    public static Delegation Create(
        Guid delegatorUserId,
        Guid delegateUserId,
        Guid permissionId,
        DateTimeOffset validFrom,
        DateTimeOffset? validTo,
        Guid? scopeUnitId = null,
        bool delegable = true)
    {
        if (delegatorUserId == Guid.Empty || delegateUserId == Guid.Empty)
        {
            throw new AuthorizationDomainException("Delegator and delegate must be valid user identifiers.");
        }

        if (delegatorUserId == delegateUserId)
        {
            throw new AuthorizationDomainException("A user cannot delegate to themselves.");
        }

        _ = new Validity(validFrom, validTo);

        return new Delegation
        {
            DelegatorUserId = delegatorUserId,
            DelegateUserId = delegateUserId,
            PermissionId = permissionId,
            ScopeUnitId = scopeUnitId,
            ValidFrom = validFrom,
            ValidTo = validTo,
            Delegable = delegable,
        };
    }

    public void Revoke()
    {
        if (IsRevoked)
        {
            throw new AuthorizationDomainException("Delegation is already revoked.");
        }

        IsRevoked = true;
        RevokedAt = DateTimeOffset.UtcNow;
    }

    public bool IsActiveAt(DateTimeOffset when) =>
        !IsRevoked && Validity.IsActiveAt(when);

    public Grant ToGrant()
    {
        if (IsRevoked)
        {
            throw new AuthorizationDomainException("Cannot produce a grant from a revoked delegation.");
        }

        if (Id == Guid.Empty)
        {
            throw new AuthorizationDomainException("Delegation must be persisted before producing a grant.");
        }

        return Grant.CreateForUser(
            DelegateUserId,
            PermissionId,
            SourceType.Delegation,
            Id,
            Effect.Allow,
            ValidFrom,
            ValidTo,
            SourcePriority.Delegation,
            scopeUnitId: ScopeUnitId);
    }
}
