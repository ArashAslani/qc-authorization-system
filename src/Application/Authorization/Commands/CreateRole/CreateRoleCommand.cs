using qc_authorization.Application.Common.Interfaces;
using qc_authorization.Domain.Authorization;
using MediatR;

namespace qc_authorization.Application.Authorization.Commands.CreateRole;

public record CreateRoleCommand(string Code, string Name, string? Description = null) : IRequest<Guid>;

public class CreateRoleCommandHandler : IRequestHandler<CreateRoleCommand, Guid>
{
    private readonly IApplicationDbContext _context;

    public CreateRoleCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Guid> Handle(CreateRoleCommand request, CancellationToken cancellationToken)
    {
        var role = Role.Create(request.Code, request.Name, request.Description);
        _context.AuthorizationRoles.Add(role);
        await _context.SaveChangesAsync(cancellationToken);
        return role.Id;
    }
}
