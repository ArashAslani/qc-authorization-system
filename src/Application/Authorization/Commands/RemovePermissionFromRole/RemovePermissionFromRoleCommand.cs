using qc_authorization.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace qc_authorization.Application.Authorization.Commands.RemovePermissionFromRole;

public record RemovePermissionFromRoleCommand(int RoleId, int PermissionId) : IRequest;

public class RemovePermissionFromRoleCommandHandler : IRequestHandler<RemovePermissionFromRoleCommand>
{
    private readonly IApplicationDbContext _context;

    public RemovePermissionFromRoleCommandHandler(IApplicationDbContext context) => _context = context;

    public async Task Handle(RemovePermissionFromRoleCommand request, CancellationToken cancellationToken)
    {
        var rp = await _context.RolePermissions
            .SingleOrDefaultAsync(x => x.RoleId == request.RoleId && x.PermissionId == request.PermissionId, cancellationToken);

        if (rp != null)
        {
            _context.RolePermissions.Remove(rp);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
