using AccessManagement.Application.Common.Interfaces;
using AccessManagement.Domain.Authorization;
using AccessManagement.Domain.Authorization.Enums;
using AccessManagement.Domain.Authorization.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace AccessManagement.Application.Authorization.Services;

public sealed class RoleGrantRematerializer
{
    private readonly IApplicationDbContext _context;

    public RoleGrantRematerializer(IApplicationDbContext context) => _context = context;

    public Task RematerializeRoleAsync(Guid roleId, CancellationToken cancellationToken) =>
        RematerializeSourceAsync(SourceType.Role, roleId, cancellationToken);

    public Task RematerializeRoleGroupAsync(Guid roleGroupId, CancellationToken cancellationToken) =>
        RematerializeSourceAsync(SourceType.RoleGroup, roleGroupId, cancellationToken);

    public async Task RematerializeRoleAndContainingGroupsAsync(Guid roleId, CancellationToken cancellationToken)
    {
        await RematerializeRoleAsync(roleId, cancellationToken);

        var groupIds = await _context.RoleGroupMembers
            .AsNoTracking()
            .Where(m => m.RoleId == roleId)
            .Select(m => m.RoleGroupId)
            .Distinct()
            .ToListAsync(cancellationToken);

        foreach (var groupId in groupIds)
        {
            await RematerializeRoleGroupAsync(groupId, cancellationToken);
        }
    }

    /// <summary>
    /// Deactivates current source grants and recreates them for every assignment × permission
    /// in a single change-tracker pass. Callers persist via <c>SaveChangesAsync</c>.
    /// TODO (performance phase, not V1): if a Role has hundreds of assignments
    /// (for example more than 500 Position/User rows), batch Deactivate+Recreate
    /// instead of loading and rewriting the full set in one transaction.
    /// </summary>
    private async Task RematerializeSourceAsync(SourceType sourceType, Guid sourceId, CancellationToken cancellationToken)
    {
        var permissionIds = sourceType == SourceType.Role
            ? await CollectPermissionIdsForRoleAsync(sourceId, cancellationToken)
            : await CollectPermissionIdsForRoleGroupAsync(sourceId, cancellationToken);

        var now = DateTimeOffset.UtcNow;
        var existing = await _context.Grants
            .Where(g => g.SourceType == sourceType
                     && g.SourceId == sourceId
                     && (g.ValidTo == null || g.ValidTo > now))
            .ToListAsync(cancellationToken);

        var assignments = existing
            .Select(g => (g.SubjectType, g.SubjectId, g.SubjectUserId, g.ValidFrom, g.ValidTo, g.ScopeUnitId))
            .Distinct()
            .ToList();

        foreach (var grant in existing)
        {
            grant.Deactivate(now);
        }

        foreach (var assignment in assignments)
        {
            foreach (var permissionId in permissionIds)
            {
                if (assignment.SubjectType == SubjectType.User && assignment.SubjectUserId is Guid userId)
                {
                    _context.Grants.Add(Grant.CreateForUser(
                        userId,
                        permissionId,
                        sourceType,
                        sourceId,
                        Effect.Allow,
                        assignment.ValidFrom,
                        assignment.ValidTo,
                        SourcePriority.RoleOrRoleGroup,
                        scopeUnitId: assignment.ScopeUnitId));
                }
                else
                {
                    _context.Grants.Add(Grant.Create(
                        assignment.SubjectType,
                        assignment.SubjectId,
                        permissionId,
                        sourceType,
                        sourceId,
                        Effect.Allow,
                        assignment.ValidFrom,
                        assignment.ValidTo,
                        SourcePriority.RoleOrRoleGroup,
                        scopeUnitId: assignment.ScopeUnitId,
                        subjectUserId: assignment.SubjectUserId));
                }
            }
        }
    }

    public async Task<HashSet<Guid>> CollectPermissionIdsForRoleAsync(Guid roleId, CancellationToken cancellationToken)
    {
        var roleIds = await FlattenRoleIdsAsync(roleId, cancellationToken);
        var permissionIds = await _context.RolePermissions
            .AsNoTracking()
            .Where(rp => roleIds.Contains(rp.RoleId))
            .Select(rp => rp.PermissionId)
            .ToListAsync(cancellationToken);
        return permissionIds.ToHashSet();
    }

    public async Task<HashSet<Guid>> CollectPermissionIdsForRoleGroupAsync(Guid roleGroupId, CancellationToken cancellationToken)
    {
        var roleIds = await _context.RoleGroupMembers
            .AsNoTracking()
            .Where(m => m.RoleGroupId == roleGroupId)
            .Select(m => m.RoleId)
            .ToListAsync(cancellationToken);

        var all = new HashSet<Guid>();
        foreach (var roleId in roleIds)
        {
            all.UnionWith(await CollectPermissionIdsForRoleAsync(roleId, cancellationToken));
        }

        return all;
    }

    private async Task<List<Guid>> FlattenRoleIdsAsync(Guid roleId, CancellationToken cancellationToken)
    {
        var ids = new List<Guid>();
        var currentId = (Guid?)roleId;
        var guard = 0;
        while (currentId is Guid id && guard++ < 32)
        {
            if (ids.Contains(id))
            {
                break;
            }

            ids.Add(id);
            currentId = await _context.AuthorizationRoles
                .AsNoTracking()
                .Where(r => r.Id == id)
                .Select(r => r.ParentRoleId)
                .SingleOrDefaultAsync(cancellationToken);
        }

        return ids;
    }
}
