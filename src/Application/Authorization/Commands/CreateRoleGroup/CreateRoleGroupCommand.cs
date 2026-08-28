using qc_authorization.Application.Common.Interfaces;
using qc_authorization.Application.Common.Interfaces.Repositories;
using qc_authorization.Domain.Authorization;
using MediatR;

namespace qc_authorization.Application.Authorization.Commands.CreateRoleGroup;

public record CreateRoleGroupCommand(string Code, string Name, string? Description = null) : IRequest<int>;

public class CreateRoleGroupCommandHandler : IRequestHandler<CreateRoleGroupCommand, int>
{
    private readonly IRoleGroupRepository _roleGroups;
    private readonly IUnitOfWork _unitOfWork;

    public CreateRoleGroupCommandHandler(IRoleGroupRepository roleGroups, IUnitOfWork unitOfWork)
    {
        _roleGroups = roleGroups;
        _unitOfWork = unitOfWork;
    }

    public async Task<int> Handle(CreateRoleGroupCommand request, CancellationToken cancellationToken)
    {
        var group = RoleGroup.Create(request.Code, request.Name, request.Description);
        await _roleGroups.AddAsync(group, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return group.Id;
    }
}
