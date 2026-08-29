using qc_authorization.Application.Authorization.Audit;
using qc_authorization.Application.Common.Interfaces;
using qc_authorization.Domain.Authorization;
using qc_authorization.Domain.Authorization.Enums;
using qc_authorization.Domain.Authorization.Exceptions;
using qc_authorization.Domain.Authorization.ValueObjects;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace qc_authorization.Application.Authorization.Commands.AssignAuthorizationRoleToPosition;

public record AssignAuthorizationRoleToPositionCommand(
    Guid PositionId,
    Guid RoleId,
    DateTimeOffset ValidFrom,
    DateTimeOffset? ValidTo = null) : IRequest;

public class AssignAuthorizationRoleToPositionCommandHandler : IRequestHandler<AssignAuthorizationRoleToPositionCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly IAuthorizationAuditService _audit;

    public AssignAuthorizationRoleToPositionCommandHandler(
        IApplicationDbContext context,
        IAuthorizationAuditService audit)
    {
        _context = context;
        _audit = audit;
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

        var existingGrants = await _context.Grants
            .Where(g => g.SubjectType == SubjectType.Position
                     && g.SubjectId == request.PositionId
                     && g.SourceType == SourceType.Role
                     && g.SourceId == request.RoleId)
            .ToListAsync(cancellationToken);

        if (existingGrants.Count > 0)
        {
            _context.Grants.RemoveRange(existingGrants);
        }

        var rolePermissions = await _context.RolePermissions
            .AsNoTracking()
            .Where(rp => rp.RoleId == request.RoleId)
            .ToListAsync(cancellationToken);

        foreach (var rolePermission in rolePermissions)
        {
            _context.Grants.Add(Grant.Create(
                SubjectType.Position,
                request.PositionId,
                rolePermission.PermissionId,
                SourceType.Role,
                request.RoleId,
                Effect.Allow,
                request.ValidFrom,
                request.ValidTo,
                SourcePriority.RoleOrRoleGroup));
        }

        await _audit.RecordAsync(
            "RoleAssignedToPosition",
            null,
            $"positionId={request.PositionId};roleId={request.RoleId};roleCode={role.Code};grantCount={rolePermissions.Count}",
            cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
