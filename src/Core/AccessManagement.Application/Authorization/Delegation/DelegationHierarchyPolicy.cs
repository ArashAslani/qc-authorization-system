using AccessManagement.Application.Common.Interfaces;
using AccessManagement.Domain.Authorization.Enums;
using AccessManagement.Domain.Authorization.Exceptions;
using AccessManagement.Domain.Authorization.ValueObjects;
using AccessManagement.Domain.Organization;
using Microsoft.EntityFrameworkCore;

namespace AccessManagement.Application.Authorization.Delegation;

public interface IDelegationHierarchyPolicy
{
    Task EnsureDelegateeIsSubordinateAsync(
        Guid delegatorUserId,
        Guid delegateUserId,
        Guid permissionId,
        DateTimeOffset when,
        CancellationToken cancellationToken = default);
}

public sealed class DelegationHierarchyPolicy : IDelegationHierarchyPolicy
{
    private readonly IApplicationDbContext _context;
    private readonly PositionHierarchyService _hierarchy;

    public DelegationHierarchyPolicy(IApplicationDbContext context, PositionHierarchyService hierarchy)
    {
        _context = context;
        _hierarchy = hierarchy;
    }

    public async Task EnsureDelegateeIsSubordinateAsync(
        Guid delegatorUserId,
        Guid delegateUserId,
        Guid permissionId,
        DateTimeOffset when,
        CancellationToken cancellationToken = default)
    {
        if (delegatorUserId == delegateUserId)
        {
            throw new AuthorizationDomainException("Delegator and delegatee must be different users.");
        }

        var hasUnboundedAccess = await _context.Grants
            .AsNoTracking()
            .AnyAsync(g => g.SubjectUserId == delegatorUserId
                        && g.PermissionId == permissionId
                        && g.Effect == Effect.Allow
                        && g.ScopeUnitId == null
                        && g.ValidFrom <= when
                        && (g.ValidTo == null || when <= g.ValidTo),
                cancellationToken);

        if (hasUnboundedAccess)
        {
            return;
        }

        var delegatorAssignments = await GetActiveAssignmentsAsync(delegatorUserId, when, cancellationToken);
        var delegateeAssignments = await GetActiveAssignmentsAsync(delegateUserId, when, cancellationToken);

        if (delegatorAssignments.Count == 0 || delegateeAssignments.Count == 0)
        {
            throw new AuthorizationDomainException(
                "Delegatee must be an organizational subordinate of the delegator.");
        }

        var allPositions = await _context.Positions.AsNoTracking().ToListAsync(cancellationToken);
        var positionsById = allPositions.ToDictionary(p => p.Id);

        foreach (var delegateeAssignment in delegateeAssignments)
        {
            if (!positionsById.TryGetValue(delegateeAssignment.PositionId, out var delegateePosition))
            {
                continue;
            }

            foreach (var delegatorAssignment in delegatorAssignments)
            {
                if (delegatorAssignment.Position.CompanyUnitId != delegateePosition.CompanyUnitId)
                {
                    continue;
                }

                if (!positionsById.TryGetValue(delegatorAssignment.PositionId, out var delegatorPosition))
                {
                    continue;
                }

                if (delegatorPosition.Id == delegateePosition.Id)
                {
                    continue;
                }

                var descendants = _hierarchy.Descendants(delegatorPosition, allPositions);
                if (descendants.Any(d => d.Id == delegateePosition.Id))
                {
                    return;
                }
            }
        }

        throw new AuthorizationDomainException(
            "Delegatee must be an organizational subordinate of the delegator.");
    }

    private async Task<List<PositionAssignment>> GetActiveAssignmentsAsync(
        Guid userId,
        DateTimeOffset when,
        CancellationToken cancellationToken)
    {
        return await _context.PositionAssignments
            .AsNoTracking()
            .Include(a => a.Position)
            .Where(a => a.Personnel.IdentityUserId == userId
                     && a.ValidFrom <= when
                     && (a.ValidTo == null || when <= a.ValidTo))
            .ToListAsync(cancellationToken);
    }
}
