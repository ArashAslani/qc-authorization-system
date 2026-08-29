using qc_authorization.Application.Common.Interfaces;
using qc_authorization.Domain.Authorization;
using qc_authorization.Domain.Authorization.Enums;
using qc_authorization.Domain.Authorization.Exceptions;
using qc_authorization.Domain.Authorization.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace qc_authorization.Application.Authorization.Services;

public sealed class RoleGroupGrantMaterializer
{
    private readonly IApplicationDbContext _context;

    public RoleGroupGrantMaterializer(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<int> MaterializeForUserAsync(
        Guid userId,
        Guid roleGroupId,
        DateTimeOffset validFrom,
        DateTimeOffset? validTo,
        CancellationToken cancellationToken)
    {
        var roleGroup = await _context.RoleGroups
            .AsNoTracking()
            .SingleOrDefaultAsync(rg => rg.Id == roleGroupId, cancellationToken)
            ?? throw new InvalidOperationException($"RoleGroup {roleGroupId} not found.");

        if (roleGroup.Status != CatalogStatus.Active)
        {
            throw new AuthorizationDomainException($"RoleGroup {roleGroup.Code} is inactive.");
        }

        var permissionIds = await CollectPermissionIdsFromMemberRolesAsync(roleGroupId, roleGroup.Code, cancellationToken);

        var existingGrants = await _context.Grants
            .Where(g => g.SubjectUserId == userId
                     && g.SourceType == SourceType.RoleGroup
                     && g.SourceId == roleGroupId)
            .ToListAsync(cancellationToken);

        if (existingGrants.Count > 0)
        {
            _context.Grants.RemoveRange(existingGrants);
        }

        foreach (var permissionId in permissionIds)
        {
            _context.Grants.Add(Grant.CreateForUser(
                userId,
                permissionId,
                SourceType.RoleGroup,
                roleGroupId,
                Effect.Allow,
                validFrom,
                validTo,
                SourcePriority.RoleOrRoleGroup));
        }

        return permissionIds.Count;
    }

    public async Task<int> MaterializeForPositionAsync(
        Guid positionId,
        Guid roleGroupId,
        DateTimeOffset validFrom,
        DateTimeOffset? validTo,
        CancellationToken cancellationToken)
    {
        var roleGroup = await _context.RoleGroups
            .AsNoTracking()
            .SingleOrDefaultAsync(rg => rg.Id == roleGroupId, cancellationToken)
            ?? throw new InvalidOperationException($"RoleGroup {roleGroupId} not found.");

        if (roleGroup.Status != CatalogStatus.Active)
        {
            throw new AuthorizationDomainException($"RoleGroup {roleGroup.Code} is inactive.");
        }

        var permissionIds = await CollectPermissionIdsFromMemberRolesAsync(roleGroupId, roleGroup.Code, cancellationToken);

        var existingGrants = await _context.Grants
            .Where(g => g.SubjectType == SubjectType.Position
                     && g.SubjectId == positionId
                     && g.SourceType == SourceType.RoleGroup
                     && g.SourceId == roleGroupId)
            .ToListAsync(cancellationToken);

        if (existingGrants.Count > 0)
        {
            _context.Grants.RemoveRange(existingGrants);
        }

        foreach (var permissionId in permissionIds)
        {
            _context.Grants.Add(Grant.Create(
                SubjectType.Position,
                positionId,
                permissionId,
                SourceType.RoleGroup,
                roleGroupId,
                Effect.Allow,
                validFrom,
                validTo,
                SourcePriority.RoleOrRoleGroup));
        }

        return permissionIds.Count;
    }

    private async Task<HashSet<Guid>> CollectPermissionIdsFromMemberRolesAsync(
        Guid roleGroupId,
        string roleGroupCode,
        CancellationToken cancellationToken)
    {
        var roleIds = await _context.RoleGroupMembers
            .AsNoTracking()
            .Where(m => m.RoleGroupId == roleGroupId)
            .Select(m => m.RoleId)
            .ToListAsync(cancellationToken);

        if (roleIds.Count == 0)
        {
            throw new InvalidOperationException($"RoleGroup {roleGroupCode} has no member roles.");
        }

        var rolePermissionIds = await _context.RolePermissions
            .AsNoTracking()
            .Where(rp => roleIds.Contains(rp.RoleId))
            .Select(rp => rp.PermissionId)
            .ToListAsync(cancellationToken);

        return rolePermissionIds.ToHashSet();
    }
}
