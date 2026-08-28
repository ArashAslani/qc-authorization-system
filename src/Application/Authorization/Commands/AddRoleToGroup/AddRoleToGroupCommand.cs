using qc_authorization.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace qc_authorization.Application.Authorization.Commands.AddRoleToGroup;

public record AddRoleToGroupCommand(int RoleGroupId, int RoleId) : IRequest;

public class AddRoleToGroupCommandHandler : IRequestHandler<AddRoleToGroupCommand>
{
    private readonly IApplicationDbContext _context;

    public AddRoleToGroupCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task Handle(AddRoleToGroupCommand request, CancellationToken cancellationToken)
    {
        var role = await _context.AuthorizationRoles
            .FirstOrDefaultAsync(r => r.Id == request.RoleId, cancellationToken)
            ?? throw new InvalidOperationException($"Role {request.RoleId} not found.");

        var group = await _context.RoleGroups
            .Include(g => g.Members)
            .FirstOrDefaultAsync(g => g.Id == request.RoleGroupId, cancellationToken)
            ?? throw new InvalidOperationException($"RoleGroup {request.RoleGroupId} not found.");

        group.AddRole(role);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
