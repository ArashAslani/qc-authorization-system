using AccessManagement.Application.Common.Interfaces;
using AccessManagement.Domain.Authorization;
using AccessManagement.Application.Common.Security;
using MediatR;

namespace AccessManagement.Application.Authorization.Commands.CreateRole;

public record CreateRoleCommand(string Code, string Name, string? Description = null, Guid? ParentRoleId = null) : IRequest<Guid>, IRequireUserAdmin;

public class CreateRoleCommandHandler : IRequestHandler<CreateRoleCommand, Guid>
{
    private readonly IApplicationDbContext _context;

    public CreateRoleCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Guid> Handle(CreateRoleCommand request, CancellationToken cancellationToken)
    {
        if (request.ParentRoleId is Guid parentId)
        {
            _ = await _context.AuthorizationRoles.FindAsync([parentId], cancellationToken)
                ?? throw new InvalidOperationException($"Parent role {parentId} was not found.");
        }

        var role = Role.Create(request.Code, request.Name, request.Description, request.ParentRoleId);
        _context.AuthorizationRoles.Add(role);
        await _context.SaveChangesAsync(cancellationToken);
        return role.Id;
    }
}
