using AccessManagement.Application.Authorization.Services;
using AccessManagement.Application.Common.Interfaces;
using AccessManagement.Application.Common.Security;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AccessManagement.Application.Authorization.Commands.RemovePermissionFromRole;

public record RemovePermissionFromRoleCommand(Guid RoleId, Guid PermissionId) : IRequest, IRequireUserAdmin;

public class RemovePermissionFromRoleCommandHandler : IRequestHandler<RemovePermissionFromRoleCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly RoleGrantRematerializer _rematerializer;

    public RemovePermissionFromRoleCommandHandler(
        IApplicationDbContext context,
        RoleGrantRematerializer rematerializer)
    {
        _context = context;
        _rematerializer = rematerializer;
    }

    public async Task Handle(RemovePermissionFromRoleCommand request, CancellationToken cancellationToken)
    {
        var rp = await _context.RolePermissions
            .SingleOrDefaultAsync(x => x.RoleId == request.RoleId && x.PermissionId == request.PermissionId, cancellationToken);

        if (rp != null)
        {
            _context.RolePermissions.Remove(rp);
            await _context.SaveChangesAsync(cancellationToken);
            await _rematerializer.RematerializeRoleAndContainingGroupsAsync(request.RoleId, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
