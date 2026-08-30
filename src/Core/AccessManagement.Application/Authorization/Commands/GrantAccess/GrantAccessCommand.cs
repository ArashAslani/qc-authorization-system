using AccessManagement.Application.Abstractions;
using AccessManagement.Application.Authorization.Audit;
using AccessManagement.Application.Authorization.Services;
using AccessManagement.Application.Common.Interfaces;
using AccessManagement.Domain.Authorization;
using AccessManagement.Domain.Authorization.Enums;
using AccessManagement.Domain.Authorization.Exceptions;
using AccessManagement.Domain.Authorization.ValueObjects;
using AccessManagement.Domain.Organization;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AccessManagement.Application.Authorization.Commands.GrantAccess;

public enum AccessGrantTargetKind
{
    Position = 0,
    User = 1,
}

public sealed record GrantAccessCommand(
    Guid ActorUserId,
    Guid ActorCompanyUnitId,
    AccessGrantTargetKind TargetKind,
    Guid TargetId,
    Guid PermissionId,
    Guid? ScopeUnitId,
    DateTimeOffset ValidFrom,
    DateTimeOffset? ValidTo) : IRequest<Guid>;

public sealed class GrantAccessCommandHandler : IRequestHandler<GrantAccessCommand, Guid>
{
    private readonly IApplicationDbContext _db;
    private readonly IActorAccessService _actorAccess;
    private readonly LineManagerTargetPolicy _targets;
    private readonly IOrganizationalUnitHierarchy _units;
    private readonly IAuthorizationAuditService _audit;

    public GrantAccessCommandHandler(
        IApplicationDbContext db,
        IActorAccessService actorAccess,
        LineManagerTargetPolicy targets,
        IOrganizationalUnitHierarchy units,
        IAuthorizationAuditService audit)
    {
        _db = db;
        _actorAccess = actorAccess;
        _targets = targets;
        _units = units;
        _audit = audit;
    }

    public async Task<Guid> Handle(GrantAccessCommand cmd, CancellationToken ct)
    {
        var permission = await _db.Permissions.FirstOrDefaultAsync(p => p.Id == cmd.PermissionId, ct)
            ?? throw new InvalidOperationException($"Permission {cmd.PermissionId} not found.");

        var isAdmin = await _actorAccess.IsUserAdminAsync(cmd.ActorUserId, cmd.ActorCompanyUnitId, ct);
        if (!isAdmin && cmd.ScopeUnitId is null)
        {
            throw new AuthorizationDomainException("ScopeUnitId is required.");
        }

        if (!isAdmin)
        {
            await EnsureLineManagerMayGrant(cmd, permission, ct);
        }

        if (cmd.ScopeUnitId is Guid scopeUnitId)
        {
            await EnsureModuleScope(permission, scopeUnitId, isAdmin, ct);
        }

        var (subjectType, subjectId, subjectUserId, sourceType, priority) = cmd.TargetKind switch
        {
            AccessGrantTargetKind.Position => (
                SubjectType.Position,
                cmd.TargetId,
                (Guid?)null,
                SourceType.Position,
                SourcePriority.PositionOverride),
            AccessGrantTargetKind.User => (
                SubjectType.User,
                Guid.Empty,
                (Guid?)cmd.TargetId,
                SourceType.User,
                SourcePriority.IndividualOverride),
            _ => throw new AuthorizationDomainException("TargetKind must be Position or User."),
        };

        var grant = subjectType == SubjectType.User
            ? Grant.CreateForUser(
                subjectUserId!.Value,
                cmd.PermissionId,
                sourceType,
                cmd.ActorUserId,
                Effect.Allow,
                cmd.ValidFrom,
                cmd.ValidTo,
                priority,
                scopeUnitId: cmd.ScopeUnitId)
            : Grant.Create(
                subjectType,
                subjectId,
                cmd.PermissionId,
                sourceType,
                cmd.ActorUserId,
                Effect.Allow,
                cmd.ValidFrom,
                cmd.ValidTo,
                priority,
                scopeUnitId: cmd.ScopeUnitId);

        _db.Grants.Add(grant);
        await _audit.RecordAsync("GrantCreated", cmd.ActorUserId, $"grant pending;target={cmd.TargetKind}:{cmd.TargetId}", ct);
        await _db.SaveChangesAsync(ct);
        return grant.Id;
    }

    private async Task EnsureLineManagerMayGrant(GrantAccessCommand cmd, Permission permission, CancellationToken ct)
    {
        var scopeUnitId = cmd.ScopeUnitId!.Value;
        if (!await _actorAccess.HasPermissionAsync(cmd.ActorUserId, cmd.ActorCompanyUnitId, CoreAccessPermissions.Grant, scopeUnitId, ct))
        {
            throw new AuthorizationDomainException("Actor is not allowed to grant access.");
        }

        var actorPositions = await _targets.GetActorPositionIdsAsync(cmd.ActorUserId, cmd.ActorCompanyUnitId, ct);
        var subordinateIds = await _targets.GetSubordinatePositionIdsAsync(actorPositions, ct);
        await _targets.EnsureTargetIsSubordinateAsync(
            cmd.TargetKind, cmd.TargetId, cmd.ActorCompanyUnitId, actorPositions, subordinateIds, ct);

        if (!await _actorAccess.HasPermissionAsync(cmd.ActorUserId, cmd.ActorCompanyUnitId, permission.Code, scopeUnitId, ct))
        {
            throw new AuthorizationDomainException("Cannot grant a permission the actor does not hold.");
        }

        var scopes = await _actorAccess.GetAccessibleRootsAsync(cmd.ActorUserId, cmd.ActorCompanyUnitId, permission.Code, ct);
        if (scopes.IsUnrestricted)
        {
            if (scopes.DeniedScopeUnitIds.Contains(scopeUnitId)
                || await IsUnderAnyAsync(scopeUnitId, scopes.DeniedScopeUnitIds, ct))
            {
                throw new AuthorizationDomainException("Requested scope is denied in the actor's effective access.");
            }

            return;
        }

        var inSubset = scopes.ScopeRootUnitIds.Contains(scopeUnitId);
        if (!inSubset)
        {
            foreach (var root in scopes.ScopeRootUnitIds)
            {
                if (await _units.IsDescendantOfAsync(scopeUnitId, root, ct))
                {
                    inSubset = true;
                    break;
                }
            }
        }

        if (!inSubset
            || scopes.DeniedScopeUnitIds.Contains(scopeUnitId)
            || await IsUnderAnyAsync(scopeUnitId, scopes.DeniedScopeUnitIds, ct))
        {
            throw new AuthorizationDomainException("Requested scope is wider than the actor's effective access.");
        }
    }

    private async Task<bool> IsUnderAnyAsync(Guid unitId, IReadOnlyList<Guid> ancestors, CancellationToken ct)
    {
        foreach (var ancestor in ancestors)
        {
            if (await _units.IsDescendantOfAsync(unitId, ancestor, ct))
            {
                return true;
            }
        }

        return false;
    }

    private async Task EnsureModuleScope(Permission permission, Guid scopeUnitId, bool isAdmin, CancellationToken ct)
    {
        if (isAdmin)
        {
            return;
        }

        var config = await _db.ModuleScopeConfigs.AsNoTracking()
            .FirstOrDefaultAsync(c => c.ResourceCode == permission.Resource, ct);
        if (config is null)
        {
            throw new AuthorizationDomainException(
                $"ModuleScopeConfig is required for non-admin writes of {permission.Resource}.");
        }

        var unitType = await _units.GetUnitTypeAsync(scopeUnitId, ct);
        if (unitType is null)
        {
            throw new AuthorizationDomainException("Scope unit was not found.");
        }

        var units = await _db.OrganizationalUnits.AsNoTracking().ToListAsync(ct);
        var scopeUnit = units.First(u => u.Id == scopeUnitId);
        var deeperThanMax = OrganizationalUnitHierarchy.Ancestors(scopeUnit, units)
            .Any(a => a.UnitType == config.MaxScopeUnitType);
        var allowed = scopeUnit.UnitType == config.MaxScopeUnitType || !deeperThanMax;

        if (!allowed)
        {
            throw new AuthorizationDomainException(
                $"Scope unit type '{unitType}' exceeds ModuleScopeConfig max '{config.MaxScopeUnitType}' for {permission.Resource}.");
        }
    }
}
