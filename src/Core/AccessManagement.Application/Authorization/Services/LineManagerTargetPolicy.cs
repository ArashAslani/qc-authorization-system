using AccessManagement.Application.Abstractions;
using AccessManagement.Application.Authorization.Commands.GrantAccess;
using AccessManagement.Application.Common.Interfaces;
using AccessManagement.Domain.Authorization.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace AccessManagement.Application.Authorization.Services;

public sealed class LineManagerTargetPolicy
{
    private readonly IApplicationDbContext _db;
    private readonly IPositionHierarchyQuery _hierarchy;

    public LineManagerTargetPolicy(IApplicationDbContext db, IPositionHierarchyQuery hierarchy)
    {
        _db = db;
        _hierarchy = hierarchy;
    }

    public async Task<IReadOnlyList<Guid>> GetActorPositionIdsAsync(
        Guid actorUserId,
        Guid companyUnitId,
        CancellationToken ct)
    {
        return await _db.PositionAssignments
            .AsNoTracking()
            .Include(a => a.Position)
            .Where(a => a.Personnel.IdentityUserId == actorUserId
                     && a.Position.CompanyUnitId == companyUnitId
                     && a.ValidTo == null)
            .Select(a => a.PositionId)
            .ToListAsync(ct);
    }

    public async Task<HashSet<Guid>> GetSubordinatePositionIdsAsync(
        IReadOnlyList<Guid> actorPositionIds,
        CancellationToken ct)
    {
        var subordinatePositionIds = new HashSet<Guid>();
        foreach (var positionId in actorPositionIds)
        {
            foreach (var descendant in await _hierarchy.GetDescendantsAsync(positionId, ct))
            {
                subordinatePositionIds.Add(descendant);
            }
        }

        return subordinatePositionIds;
    }

    public async Task EnsureTargetIsSubordinateAsync(
        AccessGrantTargetKind targetKind,
        Guid targetId,
        Guid actorCompanyUnitId,
        IReadOnlyList<Guid> actorPositionIds,
        HashSet<Guid> subordinatePositionIds,
        CancellationToken ct)
    {
        if (actorPositionIds.Count == 0)
        {
            throw new AuthorizationDomainException("Actor has no active position in the current company.");
        }

        if (targetKind == AccessGrantTargetKind.Position)
        {
            if (!subordinatePositionIds.Contains(targetId) || actorPositionIds.Contains(targetId))
            {
                throw new AuthorizationDomainException("Target position is not a subordinate in the current company.");
            }

            var target = await _db.Positions.AsNoTracking().FirstOrDefaultAsync(p => p.Id == targetId, ct)
                ?? throw new AuthorizationDomainException("Target position not found.");
            if (target.CompanyUnitId != actorCompanyUnitId)
            {
                throw new AuthorizationDomainException("Target position is in another company.");
            }

            return;
        }

        var assigned = await _db.PositionAssignments
            .AsNoTracking()
            .AnyAsync(a => a.Personnel.IdentityUserId == targetId
                        && a.ValidTo == null
                        && subordinatePositionIds.Contains(a.PositionId), ct);
        if (!assigned)
        {
            throw new AuthorizationDomainException("Target user is not assigned to a subordinate position.");
        }
    }
}
