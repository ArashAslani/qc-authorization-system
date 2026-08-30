using AccessManagement.Application.Authorization.Services;
using AccessManagement.Application.Common.Interfaces;
using AccessManagement.Application.Common.Security;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AccessManagement.Application.Authorization.Commands.RemoveRoleFromGroup;

public record RemoveRoleFromGroupCommand(Guid RoleGroupId, Guid RoleId) : IRequest, IRequireUserAdmin;

public class RemoveRoleFromGroupCommandHandler : IRequestHandler<RemoveRoleFromGroupCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly RoleGrantRematerializer _rematerializer;

    public RemoveRoleFromGroupCommandHandler(
        IApplicationDbContext context,
        RoleGrantRematerializer rematerializer)
    {
        _context = context;
        _rematerializer = rematerializer;
    }

    public async Task Handle(RemoveRoleFromGroupCommand request, CancellationToken cancellationToken)
    {
        var member = await _context.RoleGroupMembers
            .SingleOrDefaultAsync(m => m.RoleGroupId == request.RoleGroupId && m.RoleId == request.RoleId, cancellationToken);

        if (member != null)
        {
            _context.RoleGroupMembers.Remove(member);
            await _context.SaveChangesAsync(cancellationToken);
            await _rematerializer.RematerializeRoleGroupAsync(request.RoleGroupId, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
