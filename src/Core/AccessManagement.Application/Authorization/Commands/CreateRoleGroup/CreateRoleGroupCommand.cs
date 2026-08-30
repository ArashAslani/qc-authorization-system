using AccessManagement.Application.Common.Interfaces;
using AccessManagement.Domain.Authorization;
using MediatR;

namespace AccessManagement.Application.Authorization.Commands.CreateRoleGroup;

public record CreateRoleGroupCommand(string Code, string Name, string? Description = null) : IRequest<Guid>;

public class CreateRoleGroupCommandHandler : IRequestHandler<CreateRoleGroupCommand, Guid>
{
    private readonly IApplicationDbContext _context;

    public CreateRoleGroupCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Guid> Handle(CreateRoleGroupCommand request, CancellationToken cancellationToken)
    {
        var group = RoleGroup.Create(request.Code, request.Name, request.Description);
        _context.RoleGroups.Add(group);
        await _context.SaveChangesAsync(cancellationToken);
        return group.Id;
    }
}
