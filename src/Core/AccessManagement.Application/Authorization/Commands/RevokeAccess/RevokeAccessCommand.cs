using AccessManagement.Application.Abstractions;
using AccessManagement.Application.Authorization.Audit;
using AccessManagement.Application.Authorization.Commands.GrantAccess;
using AccessManagement.Application.Authorization.Services;
using AccessManagement.Application.Common.Interfaces;
using AccessManagement.Domain.Authorization;
using AccessManagement.Domain.Authorization.Enums;
using AccessManagement.Domain.Authorization.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AccessManagement.Application.Authorization.Commands.RevokeAccess;

public sealed record RevokeAccessCommand(
    Guid ActorUserId,
    Guid ActorCompanyUnitId,
    AccessGrantTargetKind TargetKind,
    Guid TargetId,
    Guid PermissionId,
    Guid? ScopeUnitId) : IRequest;

public sealed class RevokeAccessCommandHandler : IRequestHandler<RevokeAccessCommand>
{
    private readonly IApplicationDbContext _db;
    private readonly IActorAccessService _actorAccess;
    private readonly LineManagerTargetPolicy _targets;
    private readonly IPositionHierarchyQuery _hierarchy;
    private readonly IAuthorizationAuditService _audit;

    public RevokeAccessCommandHandler(
        IApplicationDbContext db,
        IActorAccessService actorAccess,
        LineManagerTargetPolicy targets,
        IPositionHierarchyQuery hierarchy,
        IAuthorizationAuditService audit)
    {
        _db = db;
        _actorAccess = actorAccess;
        _targets = targets;
        _hierarchy = hierarchy;
        _audit = audit;
    }

    public async Task Handle(RevokeAccessCommand cmd, CancellationToken ct)
    {
        _ = await _db.Permissions.FirstOrDefaultAsync(p => p.Id == cmd.PermissionId, ct)
            ?? throw new InvalidOperationException($"Permission {cmd.PermissionId} not found.");

        var isAdmin = await _actorAccess.IsUserAdminAsync(cmd.ActorUserId, cmd.ActorCompanyUnitId, ct);
        HashSet<Guid>? managerSubtree = null;

        if (!isAdmin)
        {
            if (cmd.ScopeUnitId is null)
            {
                throw new AuthorizationDomainException("ScopeUnitId is required.");
            }

            if (!await _actorAccess.HasPermissionAsync(
                    cmd.ActorUserId, cmd.ActorCompanyUnitId, CoreAccessPermissions.Revoke, cmd.ScopeUnitId, ct))
            {
                throw new AuthorizationDomainException("Actor is not allowed to revoke access.");
            }

            var actorPositions = await _targets.GetActorPositionIdsAsync(cmd.ActorUserId, cmd.ActorCompanyUnitId, ct);
            var subordinateIds = await _targets.GetSubordinatePositionIdsAsync(actorPositions, ct);
            await _targets.EnsureTargetIsSubordinateAsync(
                cmd.TargetKind, cmd.TargetId, cmd.ActorCompanyUnitId, actorPositions, subordinateIds, ct);

            var permission = await _db.Permissions.FirstAsync(p => p.Id == cmd.PermissionId, ct);
            if (!await _actorAccess.HasPermissionAsync(
                    cmd.ActorUserId, cmd.ActorCompanyUnitId, permission.Code, cmd.ScopeUnitId, ct))
            {
                throw new AuthorizationDomainException("Cannot revoke a permission the actor does not hold.");
            }

            managerSubtree = subordinateIds;
            foreach (var positionId in actorPositions)
            {
                managerSubtree.Add(positionId);
            }
        }

        var now = DateTimeOffset.UtcNow;
        if (cmd.TargetKind == AccessGrantTargetKind.Position)
        {
            var descendantIds = await _hierarchy.GetDescendantsAsync(cmd.TargetId, ct);
            var affected = descendantIds.Append(cmd.TargetId).Distinct().ToList();
            if (managerSubtree is not null)
            {
                affected = affected.Where(id => managerSubtree.Contains(id) || id == cmd.TargetId).ToList();
            }

            var grants = await _db.Grants
                .Where(g => g.SubjectType == SubjectType.Position
                         && affected.Contains(g.SubjectId)
                         && g.PermissionId == cmd.PermissionId
                         && g.ScopeUnitId == cmd.ScopeUnitId
                         && (g.ValidTo == null || g.ValidTo > now))
                .ToListAsync(ct);

            foreach (var grant in grants)
            {
                grant.Deactivate(now);
            }
        }
        else
        {
            var grants = await _db.Grants
                .Where(g => g.SubjectType == SubjectType.User
                         && g.SubjectUserId == cmd.TargetId
                         && g.PermissionId == cmd.PermissionId
                         && g.ScopeUnitId == cmd.ScopeUnitId
                         && (g.ValidTo == null || g.ValidTo > now))
                .ToListAsync(ct);

            foreach (var grant in grants)
            {
                grant.Deactivate(now);
            }
        }

        await _audit.RecordAsync(
            "AccessRevoked",
            cmd.ActorUserId,
            $"target={cmd.TargetKind}:{cmd.TargetId};permissionId={cmd.PermissionId}",
            ct);
        await _db.SaveChangesAsync(ct);
    }
}
