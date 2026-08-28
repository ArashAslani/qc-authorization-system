using qc_authorization.Application.Common.Interfaces;
using qc_authorization.Domain.Authorization;
using MediatR;

namespace qc_authorization.Application.Authorization.Commands.CreateRoleGroup;

public record CreateRoleGroupCommand(string Code, string Name, string? Description = null) : IRequest<int>;

public class CreateRoleGroupCommandHandler : IRequestHandler<CreateRoleGroupCommand, int>
{
    private readonly IApplicationDbContext _context;

    public CreateRoleGroupCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<int> Handle(CreateRoleGroupCommand request, CancellationToken cancellationToken)
    {
        var group = RoleGroup.Create(request.Code, request.Name, request.Description);
        _context.RoleGroups.Add(group);
        await _context.SaveChangesAsync(cancellationToken);
        return group.Id;
    }
}
