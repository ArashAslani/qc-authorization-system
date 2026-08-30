using AccessManagement.Application.Common.Interfaces;
using AccessManagement.Domain.Authorization;
using AccessManagement.Domain.Authorization.Enums;
using AccessManagement.Domain.Authorization.Exceptions;
using AccessManagement.Domain.Authorization.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace AccessManagement.Application.Authorization.Services;

public sealed class RoleGroupGrantMaterializer
{
    private readonly IApplicationDbContext _context;
    private readonly RoleGrantRematerializer _rematerializer;

    public RoleGroupGrantMaterializer(IApplicationDbContext context, RoleGrantRematerializer rematerializer)
    {
        _context = context;
        _rematerializer = rematerializer;
    }

    public async Task<int> MaterializeForUserAsync(
        Guid userId,
        Guid roleGroupId,
        DateTimeOffset validFrom,
        DateTimeOffset? validTo,
        Guid? scopeUnitId,
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

        var permissionIds = await CollectOrThrowAsync(roleGroup.Code, roleGroupId, cancellationToken);
        var now = DateTimeOffset.UtcNow;
        var existingGrants = await _context.Grants
            .Where(g => g.SubjectUserId == userId
                     && g.SourceType == SourceType.RoleGroup
                     && g.SourceId == roleGroupId
                     && (g.ValidTo == null || g.ValidTo > now))
            .ToListAsync(cancellationToken);

        foreach (var grant in existingGrants)
        {
            grant.Deactivate(now);
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
                SourcePriority.RoleOrRoleGroup,
                scopeUnitId: scopeUnitId));
        }

        return permissionIds.Count;
    }

    public async Task<int> MaterializeForPositionAsync(
        Guid positionId,
        Guid roleGroupId,
        DateTimeOffset validFrom,
        DateTimeOffset? validTo,
        Guid? scopeUnitId,
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

        var permissionIds = await CollectOrThrowAsync(roleGroup.Code, roleGroupId, cancellationToken);
        var now = DateTimeOffset.UtcNow;
        var existingGrants = await _context.Grants
            .Where(g => g.SubjectType == SubjectType.Position
                     && g.SubjectId == positionId
                     && g.SourceType == SourceType.RoleGroup
                     && g.SourceId == roleGroupId
                     && (g.ValidTo == null || g.ValidTo > now))
            .ToListAsync(cancellationToken);

        foreach (var grant in existingGrants)
        {
            grant.Deactivate(now);
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
                SourcePriority.RoleOrRoleGroup,
                scopeUnitId: scopeUnitId));
        }

        return permissionIds.Count;
    }

    private async Task<IReadOnlyCollection<Guid>> CollectOrThrowAsync(
        string roleGroupCode,
        Guid roleGroupId,
        CancellationToken cancellationToken)
    {
        var permissionIds = await _rematerializer.CollectPermissionIdsForRoleGroupAsync(roleGroupId, cancellationToken);
        if (permissionIds.Count == 0)
        {
            throw new InvalidOperationException($"RoleGroup {roleGroupCode} has no member roles.");
        }

        return permissionIds;
    }
}
