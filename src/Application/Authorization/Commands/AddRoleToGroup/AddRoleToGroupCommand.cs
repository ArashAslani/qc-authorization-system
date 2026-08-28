using qc_authorization.Application.Common.Interfaces;
using qc_authorization.Application.Common.Interfaces.Repositories;
using MediatR;

namespace qc_authorization.Application.Authorization.Commands.AddRoleToGroup;

public record AddRoleToGroupCommand(int RoleGroupId, int RoleId) : IRequest;

public class AddRoleToGroupCommandHandler : IRequestHandler<AddRoleToGroupCommand>
{
    private readonly IRoleGroupRepository _roleGroups;
    private readonly IRoleRepository _roles;
    private readonly IUnitOfWork _unitOfWork;

    public AddRoleToGroupCommandHandler(
        IRoleGroupRepository roleGroups,
        IRoleRepository roles,
        IUnitOfWork unitOfWork)
    {
        _roleGroups = roleGroups;
        _roles = roles;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(AddRoleToGroupCommand request, CancellationToken cancellationToken)
    {
        var role = await _roles.GetByIdAsync(request.RoleId, cancellationToken)
            ?? throw new InvalidOperationException($"Role {request.RoleId} not found.");

        var group = await _roleGroups.GetByIdAsync(request.RoleGroupId, cancellationToken)
            ?? throw new InvalidOperationException($"RoleGroup {request.RoleGroupId} not found.");

        group.AddRole(role);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
