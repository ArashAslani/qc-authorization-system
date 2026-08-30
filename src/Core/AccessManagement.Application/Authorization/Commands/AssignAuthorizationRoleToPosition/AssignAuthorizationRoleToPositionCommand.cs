using AccessManagement.Application.Authorization.Audit;
using AccessManagement.Application.Authorization.Services;
using AccessManagement.Application.Common.Interfaces;
using AccessManagement.Domain.Authorization;
using AccessManagement.Domain.Authorization.Enums;
using AccessManagement.Domain.Authorization.Exceptions;
using AccessManagement.Domain.Authorization.ValueObjects;
using AccessManagement.Application.Common.Security;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AccessManagement.Application.Authorization.Commands.AssignAuthorizationRoleToPosition;

public record AssignAuthorizationRoleToPositionCommand(
    Guid PositionId,
    Guid RoleId,
    DateTimeOffset ValidFrom,
    DateTimeOffset? ValidTo = null,
    Guid? ScopeUnitId = null) : IRequest, IRequireUserAdmin;

public class AssignAuthorizationRoleToPositionCommandHandler : IRequestHandler<AssignAuthorizationRoleToPositionCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly IAuthorizationAuditService _audit;
    private readonly RoleGrantRematerializer _rematerializer;

    public AssignAuthorizationRoleToPositionCommandHandler(
        IApplicationDbContext context,
        IAuthorizationAuditService audit,
        RoleGrantRematerializer rematerializer)
    {
        _context = context;
        _audit = audit;
        _rematerializer = rematerializer;
    }

    public async Task Handle(AssignAuthorizationRoleToPositionCommand request, CancellationToken cancellationToken)
    {
        _ = await _context.Positions
            .AsNoTracking()
            .SingleOrDefaultAsync(p => p.Id == request.PositionId, cancellationToken)
            ?? throw new InvalidOperationException($"Position {request.PositionId} not found.");

        var role = await _context.AuthorizationRoles
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == request.RoleId, cancellationToken)
            ?? throw new InvalidOperationException($"Role {request.RoleId} not found.");

        if (role.Status != CatalogStatus.Active)
        {
            throw new AuthorizationDomainException($"Role {role.Code} is inactive.");
        }

        var now = DateTimeOffset.UtcNow;
        var existingGrants = await _context.Grants
            .Where(g => g.SubjectType == SubjectType.Position
                     && g.SubjectId == request.PositionId
                     && g.SourceType == SourceType.Role
                     && g.SourceId == request.RoleId
                     && (g.ValidTo == null || g.ValidTo > now))
            .ToListAsync(cancellationToken);

        foreach (var grant in existingGrants)
        {
            grant.Deactivate(now);
        }

        var permissionIds = await _rematerializer.CollectPermissionIdsForRoleAsync(request.RoleId, cancellationToken);
        foreach (var permissionId in permissionIds)
        {
            _context.Grants.Add(Grant.Create(
                SubjectType.Position,
                request.PositionId,
                permissionId,
                SourceType.Role,
                request.RoleId,
                Effect.Allow,
                request.ValidFrom,
                request.ValidTo,
                SourcePriority.RoleOrRoleGroup,
                scopeUnitId: request.ScopeUnitId));
        }

        await _audit.RecordAsync(
            "RoleAssignedToPosition",
            null,
            $"positionId={request.PositionId};roleId={request.RoleId};roleCode={role.Code};grantCount={permissionIds.Count}",
            cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
