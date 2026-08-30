using AccessManagement.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AccessManagement.Application.Authorization.Commands.RemoveRoleFromGroup;

public record RemoveRoleFromGroupCommand(Guid RoleGroupId, Guid RoleId) : IRequest;

public class RemoveRoleFromGroupCommandHandler : IRequestHandler<RemoveRoleFromGroupCommand>
{
    private readonly IApplicationDbContext _context;

    public RemoveRoleFromGroupCommandHandler(IApplicationDbContext context) => _context = context;

    public async Task Handle(RemoveRoleFromGroupCommand request, CancellationToken cancellationToken)
    {
        var member = await _context.RoleGroupMembers
            .SingleOrDefaultAsync(m => m.RoleGroupId == request.RoleGroupId && m.RoleId == request.RoleId, cancellationToken);

        if (member != null)
        {
            _context.RoleGroupMembers.Remove(member);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
