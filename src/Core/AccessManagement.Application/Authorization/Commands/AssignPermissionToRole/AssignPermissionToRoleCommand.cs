using AccessManagement.Application.Authorization.Services;
using AccessManagement.Application.Common.Interfaces;
using AccessManagement.Domain.Authorization;
using AccessManagement.Application.Common.Security;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AccessManagement.Application.Authorization.Commands.AssignPermissionToRole;

public record AssignPermissionToRoleCommand(Guid RoleId, Guid PermissionId) : IRequest, IRequireUserAdmin;

public class AssignPermissionToRoleCommandHandler : IRequestHandler<AssignPermissionToRoleCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly RoleGrantRematerializer _rematerializer;

    public AssignPermissionToRoleCommandHandler(
        IApplicationDbContext context,
        RoleGrantRematerializer rematerializer)
    {
        _context = context;
        _rematerializer = rematerializer;
    }

    public async Task Handle(AssignPermissionToRoleCommand request, CancellationToken cancellationToken)
    {
        _ = await _context.AuthorizationRoles
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == request.RoleId, cancellationToken)
            ?? throw new InvalidOperationException($"Role {request.RoleId} not found.");

        _ = await _context.Permissions
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == request.PermissionId, cancellationToken)
            ?? throw new InvalidOperationException($"Permission {request.PermissionId} not found.");

        _context.RolePermissions.Add(new RolePermission
        {
            RoleId = request.RoleId,
            PermissionId = request.PermissionId,
        });

        await _context.SaveChangesAsync(cancellationToken);
        await _rematerializer.RematerializeRoleAndContainingGroupsAsync(request.RoleId, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
