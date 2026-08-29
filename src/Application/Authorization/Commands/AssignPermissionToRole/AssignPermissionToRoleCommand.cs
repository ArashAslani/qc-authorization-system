using qc_authorization.Application.Common.Interfaces;
using qc_authorization.Domain.Authorization;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace qc_authorization.Application.Authorization.Commands.AssignPermissionToRole;

public record AssignPermissionToRoleCommand(Guid RoleId, Guid PermissionId) : IRequest;

public class AssignPermissionToRoleCommandHandler : IRequestHandler<AssignPermissionToRoleCommand>
{
    private readonly IApplicationDbContext _context;

    public AssignPermissionToRoleCommandHandler(IApplicationDbContext context)
    {
        _context = context;
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
    }
}
