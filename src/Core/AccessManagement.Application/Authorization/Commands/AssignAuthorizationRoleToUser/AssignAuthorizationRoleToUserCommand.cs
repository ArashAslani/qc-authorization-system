using AccessManagement.Application.Authorization.Audit;
using AccessManagement.Application.Common.Interfaces;
using AccessManagement.Domain.Authorization;
using AccessManagement.Domain.Authorization.Enums;
using AccessManagement.Domain.Authorization.Exceptions;
using AccessManagement.Domain.Authorization.ValueObjects;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AccessManagement.Application.Authorization.Commands.AssignAuthorizationRoleToUser;

public record AssignAuthorizationRoleToUserCommand(
    Guid UserId,
    Guid RoleId,
    DateTimeOffset ValidFrom,
    DateTimeOffset? ValidTo = null) : IRequest;

public class AssignAuthorizationRoleToUserCommandHandler : IRequestHandler<AssignAuthorizationRoleToUserCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly IAuthorizationAuditService _audit;

    public AssignAuthorizationRoleToUserCommandHandler(
        IApplicationDbContext context,
        IAuthorizationAuditService audit)
    {
        _context = context;
        _audit = audit;
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

        var existingGrants = await _context.Grants
            .Where(g => g.SubjectUserId == request.UserId
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
            var grant = Grant.CreateForUser(
                request.UserId,
                rolePermission.PermissionId,
                SourceType.Role,
                request.RoleId,
                Effect.Allow,
                request.ValidFrom,
                request.ValidTo,
                SourcePriority.RoleOrRoleGroup);

            _context.Grants.Add(grant);
        }

        await _audit.RecordAsync(
            "RoleAssignedToUser",
            null,
            $"userId={request.UserId};roleId={request.RoleId};roleCode={role.Code};grantCount={rolePermissions.Count}",
            cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
