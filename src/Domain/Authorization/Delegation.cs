using qc_authorization.Domain.Authorization.Enums;
using qc_authorization.Domain.Authorization.Exceptions;
using qc_authorization.Domain.Authorization.ValueObjects;
using qc_authorization.Domain.Common;

namespace qc_authorization.Domain.Authorization;

/// <summary>
/// Delegation produces a <see cref="Grant"/> with <c>SourceType = Delegation</c>.
/// It never decides Allow/Deny; the Access Evaluation Engine does.
/// </summary>
public class Delegation : BaseAuditableEntity, IAggregateRoot
{
    private Delegation() { }

    public int DelegatorUserId { get; private set; }
    public int DelegateUserId { get; private set; }

    public int PermissionId { get; private set; }
    public Permission Permission { get; private set; } = null!;

    public ScopeKind ScopeKind { get; private set; } = ScopeKind.Unbounded;
    public string? ScopeIdentifier { get; private set; }

    public DateTimeOffset ValidFrom { get; private set; }
    public DateTimeOffset? ValidTo { get; private set; }

    public bool Delegable { get; private set; } = true;

    public bool IsRevoked { get; private set; }

    public Validity Validity => new(ValidFrom, ValidTo);

    public static Delegation Create(
        int delegatorUserId,
        int delegateUserId,
        int permissionId,
        DateTimeOffset validFrom,
        DateTimeOffset? validTo,
        ScopeKind scopeKind = ScopeKind.Unbounded,
        string? scopeIdentifier = null,
        bool delegable = true)
    {
        if (delegatorUserId <= 0 || delegateUserId <= 0)
        {
            throw new AuthorizationDomainException("Delegator and delegate must be valid user identifiers.");
        }

        if (delegatorUserId == delegateUserId)
        {
            throw new AuthorizationDomainException("A user cannot delegate to themselves.");
        }

        _ = new Validity(validFrom, validTo);
        _ = new Scope(scopeKind, scopeIdentifier);

        return new Delegation
        {
            DelegatorUserId = delegatorUserId,
            DelegateUserId = delegateUserId,
            PermissionId = permissionId,
            ScopeKind = scopeKind,
            ScopeIdentifier = scopeIdentifier,
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
    }

    public bool IsActiveAt(DateTimeOffset when) =>
        !IsRevoked && Validity.IsActiveAt(when);

    /// <summary>
    /// Materializes the grant fact this delegation contributes at evaluation time.
    /// </summary>
    public Grant ToGrant()
    {
        if (IsRevoked)
        {
            throw new AuthorizationDomainException("Cannot produce a grant from a revoked delegation.");
        }

        if (Id <= 0)
        {
            throw new AuthorizationDomainException("Delegation must be persisted before producing a grant.");
        }

        return Grant.Create(
            SubjectType.User,
            DelegateUserId,
            PermissionId,
            SourceType.Delegation,
            Id,
            Effect.Allow,
            ValidFrom,
            ValidTo,
            SourcePriority.Delegation,
            scopeKind: ScopeKind,
            scopeIdentifier: ScopeIdentifier);
    }
}
