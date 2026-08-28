using qc_authorization.Application.Common.Interfaces;
using qc_authorization.Application.Common.Interfaces.Repositories;
using qc_authorization.Domain.Authorization;
using MediatR;

namespace qc_authorization.Application.Authorization.Commands.AssignPermissionToRole;

public record AssignPermissionToRoleCommand(int RoleId, int PermissionId) : IRequest;

public class AssignPermissionToRoleCommandHandler : IRequestHandler<AssignPermissionToRoleCommand>
{
    private readonly IRoleRepository _roles;
    private readonly IPermissionRepository _permissions;
    private readonly IUnitOfWork _unitOfWork;

    public AssignPermissionToRoleCommandHandler(
        IRoleRepository roles,
        IPermissionRepository permissions,
        IUnitOfWork unitOfWork)
    {
        _roles = roles;
        _permissions = permissions;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(AssignPermissionToRoleCommand request, CancellationToken cancellationToken)
    {
        _ = await _roles.GetByIdAsync(request.RoleId, cancellationToken)
            ?? throw new InvalidOperationException($"Role {request.RoleId} not found.");
        _ = await _permissions.GetByIdAsync(request.PermissionId, cancellationToken)
            ?? throw new InvalidOperationException($"Permission {request.PermissionId} not found.");

        await _roles.AddPermissionAsync(new RolePermission
        {
            RoleId = request.RoleId,
            PermissionId = request.PermissionId,
        }, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
