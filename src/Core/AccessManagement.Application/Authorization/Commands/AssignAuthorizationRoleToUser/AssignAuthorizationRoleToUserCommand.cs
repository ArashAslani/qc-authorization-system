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

namespace AccessManagement.Application.Authorization.Commands.AssignAuthorizationRoleToUser;

public record AssignAuthorizationRoleToUserCommand(
    Guid UserId,
    Guid RoleId,
    DateTimeOffset ValidFrom,
    DateTimeOffset? ValidTo = null,
    Guid? ScopeUnitId = null) : IRequest, IRequireUserAdmin;

public class AssignAuthorizationRoleToUserCommandHandler : IRequestHandler<AssignAuthorizationRoleToUserCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly IAuthorizationAuditService _audit;
    private readonly RoleGrantRematerializer _rematerializer;

    public AssignAuthorizationRoleToUserCommandHandler(
        IApplicationDbContext context,
        IAuthorizationAuditService audit,
        RoleGrantRematerializer rematerializer)
    {
        _context = context;
        _audit = audit;
        _rematerializer = rematerializer;
    }

    public async Task Handle(AssignAuthorizationRoleToUserCommand request, CancellationToken cancellationToken)
    {
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
            .Where(g => g.SubjectUserId == request.UserId
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
            _context.Grants.Add(Grant.CreateForUser(
                request.UserId,
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
            "RoleAssignedToUser",
            null,
            $"userId={request.UserId};roleId={request.RoleId};roleCode={role.Code};grantCount={permissionIds.Count}",
            cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
