using AccessManagement.Application.Authorization.Services;
using AccessManagement.Application.Common.Interfaces;
using AccessManagement.Domain.Authorization;
using AccessManagement.Domain.Authorization.Enums;
using AccessManagement.Domain.Authorization.Exceptions;
using AccessManagement.Domain.Organization.Enums;
using Microsoft.EntityFrameworkCore;

namespace AccessManagement.Application.Authorization.Delegation;

public interface IDelegationSubsetPolicy
{
    Task EnsureDelegatorCanDelegateAsync(
        Guid delegatorUserId,
        Guid permissionId,
        Guid? scopeUnitId,
        DateTimeOffset when,
        Guid? delegatorCompanyUnitId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Latest remaining expiry of the delegator's covering Allow access at <paramref name="when"/>.
    /// Null means unbounded.
    /// </summary>
    Task<DateTimeOffset?> ResolveDelegatorAccessExpiryAsync(
        Guid delegatorUserId,
        Guid permissionId,
        Guid? scopeUnitId,
        DateTimeOffset when,
        Guid? delegatorCompanyUnitId = null,
        CancellationToken cancellationToken = default);
}

public sealed class DelegationSubsetPolicy : IDelegationSubsetPolicy
{
    private readonly IActorAccessService _actorAccess;
    private readonly IApplicationDbContext _context;

    public DelegationSubsetPolicy(
        IActorAccessService actorAccess,
        IApplicationDbContext context)
    {
        _actorAccess = actorAccess;
        _context = context;
    }

    public async Task EnsureDelegatorCanDelegateAsync(
        Guid delegatorUserId,
        Guid permissionId,
        Guid? scopeUnitId,
        DateTimeOffset when,
        Guid? delegatorCompanyUnitId = null,
        CancellationToken cancellationToken = default)
    {
        var permission = await _context.Permissions
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == permissionId, cancellationToken)
            ?? throw new InvalidOperationException($"Permission {permissionId} not found.");

        var covering = await LoadCoveringAllowExpiriesAsync(
            delegatorUserId, permissionId, scopeUnitId, when, delegatorCompanyUnitId, cancellationToken);

        if (covering.Count > 0)
        {
            return;
        }

        var allowedNow = await _actorAccess.HasPermissionAsync(
            delegatorUserId,
            delegatorCompanyUnitId,
            permission.Code,
            scopeUnitId,
            cancellationToken);
        if (!allowedNow)
        {
            throw new AuthorizationDomainException(
                "Delegator does not have effective access required to delegate this permission.");
        }
    }

    public async Task<DateTimeOffset?> ResolveDelegatorAccessExpiryAsync(
        Guid delegatorUserId,
        Guid permissionId,
        Guid? scopeUnitId,
        DateTimeOffset when,
        Guid? delegatorCompanyUnitId = null,
        CancellationToken cancellationToken = default)
    {
        var covering = await LoadCoveringAllowExpiriesAsync(
            delegatorUserId, permissionId, scopeUnitId, when, delegatorCompanyUnitId, cancellationToken);

        if (covering.Count == 0 || covering.Any(v => v is null))
        {
            return null;
        }

        return covering.Max();
    }

    private async Task<List<DateTimeOffset?>> LoadCoveringAllowExpiriesAsync(
        Guid delegatorUserId,
        Guid permissionId,
        Guid? scopeUnitId,
        DateTimeOffset when,
        Guid? companyUnitId,
        CancellationToken cancellationToken)
    {
        var expiries = new List<DateTimeOffset?>();

        var userGrants = await _context.Grants
            .AsNoTracking()
            .Where(g => g.SubjectUserId == delegatorUserId
                     && g.PermissionId == permissionId
                     && g.Effect == Effect.Allow
                     && g.ValidFrom <= when
                     && (g.ValidTo == null || when <= g.ValidTo))
            .Select(g => new { g.ScopeUnitId, g.ValidTo })
            .ToListAsync(cancellationToken);

        foreach (var grant in userGrants)
        {
            if (ScopeCovers(grant.ScopeUnitId, scopeUnitId))
            {
                expiries.Add(grant.ValidTo);
            }
        }

        var assignmentQuery = _context.PositionAssignments
            .AsNoTracking()
            .Where(a => a.Personnel.IdentityUserId == delegatorUserId
                     && a.Personnel.Status == PersonnelStatus.Active
                     && a.Position.Status == PositionStatus.Active
                     && a.ValidFrom <= when
                     && (a.ValidTo == null || when <= a.ValidTo));
        if (companyUnitId is Guid company)
        {
            assignmentQuery = assignmentQuery.Where(a => a.Position.CompanyUnitId == company);
        }

        var positionIds = await assignmentQuery.Select(a => a.PositionId).ToListAsync(cancellationToken);
        if (positionIds.Count > 0)
        {
            var positionGrants = await _context.Grants
                .AsNoTracking()
                .Where(g => g.SubjectType == SubjectType.Position
                         && positionIds.Contains(g.SubjectId)
                         && g.PermissionId == permissionId
                         && g.Effect == Effect.Allow
                         && g.ValidFrom <= when
                         && (g.ValidTo == null || when <= g.ValidTo))
                .Select(g => new { g.ScopeUnitId, g.ValidTo })
                .ToListAsync(cancellationToken);

            foreach (var grant in positionGrants)
            {
                if (ScopeCovers(grant.ScopeUnitId, scopeUnitId))
                {
                    expiries.Add(grant.ValidTo);
                }
            }
        }

        var inbound = await _context.Delegations
            .AsNoTracking()
            .Where(d => d.DelegateUserId == delegatorUserId
                     && d.PermissionId == permissionId
                     && !d.IsRevoked
                     && d.ValidFrom <= when
                     && (d.ValidTo == null || when <= d.ValidTo))
            .Select(d => new { d.ScopeUnitId, d.ValidTo })
            .ToListAsync(cancellationToken);

        foreach (var delegation in inbound)
        {
            if (ScopeCovers(delegation.ScopeUnitId, scopeUnitId))
            {
                expiries.Add(delegation.ValidTo);
            }
        }

        return expiries;
    }

    private static bool ScopeCovers(Guid? grantScope, Guid? requestedScope)
    {
        if (grantScope is null)
        {
            return true;
        }

        if (requestedScope is null)
        {
            return false;
        }

        return grantScope == requestedScope;
    }
}
