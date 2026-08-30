using AccessManagement.Application.Abstractions;
using AccessManagement.Application.Authorization.Audit;
using AccessManagement.Application.Common.Interfaces;
using AccessManagement.Domain.Authorization;
using AccessManagement.Domain.Authorization.Enums;
using AccessManagement.Domain.Authorization.Evaluation;
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
    Guid ScopeUnitId,
    DateTimeOffset ValidFrom,
    DateTimeOffset? ValidTo) : IRequest<Guid>;

public sealed class GrantAccessCommandHandler : IRequestHandler<GrantAccessCommand, Guid>
{
    private readonly IApplicationDbContext _db;
    private readonly IAccessEvaluator _evaluator;
    private readonly IPositionHierarchyQuery _hierarchy;
    private readonly IOrganizationalUnitHierarchy _units;
    private readonly IAuthorizationAuditService _audit;

    public GrantAccessCommandHandler(
        IApplicationDbContext db,
        IAccessEvaluator evaluator,
        IPositionHierarchyQuery hierarchy,
        IOrganizationalUnitHierarchy units,
        IAuthorizationAuditService audit)
    {
        _db = db;
        _evaluator = evaluator;
        _hierarchy = hierarchy;
        _units = units;
        _audit = audit;
    }

    public async Task<Guid> Handle(GrantAccessCommand cmd, CancellationToken ct)
    {
        var permission = await _db.Permissions.FirstOrDefaultAsync(p => p.Id == cmd.PermissionId, ct)
            ?? throw new InvalidOperationException($"Permission {cmd.PermissionId} not found.");

        var actorPersonnel = await _db.Personnel
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.IdentityUserId == cmd.ActorUserId, ct);

        var isAdmin = actorPersonnel is { IsSystemUser: true }
            || await HasAdministerAll(cmd, ct);

        if (!isAdmin)
        {
            await EnsureLineManagerMayGrant(cmd, permission, actorPersonnel, ct);
        }

        await EnsureModuleScope(permission, cmd.ScopeUnitId, isAdmin, ct);

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

    private async Task<bool> HasAdministerAll(GrantAccessCommand cmd, CancellationToken ct)
    {
        var decision = await _evaluator.EvaluateAsync(
            new AccessRequest(cmd.ActorUserId, null, CoreAccessPermissions.AdministerAll, cmd.ActorCompanyUnitId, DateTimeOffset.UtcNow),
            ct);
        return decision.Allowed;
    }

    private async Task EnsureLineManagerMayGrant(
        GrantAccessCommand cmd,
        Permission permission,
        Personnel? actorPersonnel,
        CancellationToken ct)
    {
        var grantPerm = await _evaluator.EvaluateAsync(
            new AccessRequest(cmd.ActorUserId, null, CoreAccessPermissions.Grant, cmd.ScopeUnitId, DateTimeOffset.UtcNow),
            ct);
        if (!grantPerm.Allowed)
        {
            throw new AuthorizationDomainException("Actor is not allowed to grant access.");
        }

        var actorPositions = await _db.PositionAssignments
            .AsNoTracking()
            .Include(a => a.Position)
            .Where(a => a.Personnel.IdentityUserId == cmd.ActorUserId
                     && a.Position.CompanyUnitId == cmd.ActorCompanyUnitId
                     && a.ValidTo == null)
            .Select(a => a.PositionId)
            .ToListAsync(ct);

        if (actorPositions.Count == 0)
        {
            throw new AuthorizationDomainException("Actor has no active position in the current company.");
        }

        var subordinatePositionIds = new HashSet<Guid>();
        foreach (var positionId in actorPositions)
        {
            foreach (var descendant in await _hierarchy.GetDescendantsAsync(positionId, ct))
            {
                subordinatePositionIds.Add(descendant);
            }
        }

        if (cmd.TargetKind == AccessGrantTargetKind.Position)
        {
            if (!subordinatePositionIds.Contains(cmd.TargetId) || actorPositions.Contains(cmd.TargetId))
            {
                throw new AuthorizationDomainException("Target position is not a subordinate in the current company.");
            }

            var target = await _db.Positions.AsNoTracking().FirstOrDefaultAsync(p => p.Id == cmd.TargetId, ct)
                ?? throw new AuthorizationDomainException("Target position not found.");
            if (target.CompanyUnitId != cmd.ActorCompanyUnitId)
            {
                throw new AuthorizationDomainException("Target position is in another company.");
            }
        }
        else
        {
            var assigned = await _db.PositionAssignments
                .AsNoTracking()
                .Include(a => a.Position)
                .AnyAsync(a => a.Personnel.IdentityUserId == cmd.TargetId
                            && a.ValidTo == null
                            && subordinatePositionIds.Contains(a.PositionId), ct);
            if (!assigned)
            {
                throw new AuthorizationDomainException("Target user is not assigned to a subordinate position.");
            }
        }

        var contentAllowed = await _evaluator.EvaluateAsync(
            new AccessRequest(cmd.ActorUserId, actorPositions[0], permission.Code, cmd.ScopeUnitId, DateTimeOffset.UtcNow),
            ct);
        if (!contentAllowed.Allowed)
        {
            throw new AuthorizationDomainException("Cannot grant a permission the actor does not hold.");
        }

        var scopes = await _evaluator.GetAccessibleScopesAsync(cmd.ActorUserId, actorPositions[0], permission.Code, ct);
        if (scopes.IsUnrestricted)
        {
            return;
        }

        var inSubset = scopes.ScopeRootUnitIds.Contains(cmd.ScopeUnitId);
        if (!inSubset)
        {
            foreach (var root in scopes.ScopeRootUnitIds)
            {
                if (await _units.IsDescendantOfAsync(cmd.ScopeUnitId, root, ct))
                {
                    inSubset = true;
                    break;
                }
            }
        }

        if (!inSubset)
        {
            throw new AuthorizationDomainException("Requested scope is wider than the actor's effective access.");
        }
    }

    private async Task EnsureModuleScope(Permission permission, Guid scopeUnitId, bool isAdmin, CancellationToken ct)
    {
        var config = await _db.ModuleScopeConfigs.AsNoTracking()
            .FirstOrDefaultAsync(c => c.ResourceCode == permission.Resource, ct);
        if (config is null)
        {
            return;
        }

        var unitType = await _units.GetUnitTypeAsync(scopeUnitId, ct);
        if (unitType is null)
        {
            throw new AuthorizationDomainException("Scope unit was not found.");
        }

        var units = await _db.OrganizationalUnits.AsNoTracking().ToListAsync(ct);
        var scopeUnit = units.First(u => u.Id == scopeUnitId);
        var allowed = isAdmin
            || scopeUnit.UnitType == config.MaxScopeUnitType
            || OrganizationalUnitHierarchy.Ancestors(scopeUnit, units).Any(a => a.UnitType == config.MaxScopeUnitType);

        if (!allowed)
        {
            throw new AuthorizationDomainException(
                $"Scope unit type '{unitType}' exceeds ModuleScopeConfig max '{config.MaxScopeUnitType}' for {permission.Resource}.");
        }
    }
}
