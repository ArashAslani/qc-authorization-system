using qc_authorization.Application.Common.Interfaces;
using qc_authorization.Domain.Authorization;
using qc_authorization.Domain.Authorization.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace qc_authorization.Application.Authorization.Commands.AddRoleToGroup;

public record AddRoleToGroupCommand(Guid RoleGroupId, Guid RoleId) : IRequest;

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
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == request.RoleId, cancellationToken)
            ?? throw new InvalidOperationException($"Role {request.RoleId} not found.");

        var groupExists = await _context.RoleGroups
            .AsNoTracking()
            .AnyAsync(g => g.Id == request.RoleGroupId, cancellationToken);

        if (!groupExists)
        {
            throw new InvalidOperationException($"RoleGroup {request.RoleGroupId} not found.");
        }

        var alreadyMember = await _context.RoleGroupMembers
            .AnyAsync(m => m.RoleGroupId == request.RoleGroupId && m.RoleId == request.RoleId, cancellationToken);

        if (alreadyMember)
        {
            throw new AuthorizationDomainException($"Role {role.Code} is already in the group.");
        }

        _context.RoleGroupMembers.Add(new RoleGroupMember
        {
            RoleGroupId = request.RoleGroupId,
            RoleId = request.RoleId,
        });

        await _context.SaveChangesAsync(cancellationToken);
    }
}
