using qc_authorization.Application.Common.Interfaces;
using qc_authorization.Application.Common.Interfaces.Repositories;
using qc_authorization.Domain.Authorization;
using MediatR;

namespace qc_authorization.Application.Authorization.Commands.CreatePermission;

public record CreatePermissionCommand(
    string ResourceCode,
    string ResourceName,
    string ActionCode,
    string ActionName,
    string? Description = null) : IRequest<int>;

public class CreatePermissionCommandHandler : IRequestHandler<CreatePermissionCommand, int>
{
    private readonly IResourceCatalogRepository _resources;
    private readonly IActionCatalogRepository _actions;
    private readonly IPermissionRepository _permissions;
    private readonly IUnitOfWork _unitOfWork;

    public CreatePermissionCommandHandler(
        IResourceCatalogRepository resources,
        IActionCatalogRepository actions,
        IPermissionRepository permissions,
        IUnitOfWork unitOfWork)
    {
        _resources = resources;
        _actions = actions;
        _permissions = permissions;
        _unitOfWork = unitOfWork;
    }

    public async Task<int> Handle(CreatePermissionCommand request, CancellationToken cancellationToken)
    {
        var resource = await _resources.GetByCodeAsync(request.ResourceCode, cancellationToken)
            ?? ResourceCatalog.Create(request.ResourceCode, request.ResourceName);
        if (resource.Id == 0)
        {
            await _resources.AddAsync(resource, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        var action = await _actions.GetByCodeAsync(request.ActionCode, cancellationToken)
            ?? ActionCatalog.Create(request.ActionCode, request.ActionName);
        if (action.Id == 0)
        {
            await _actions.AddAsync(action, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        var permission = Permission.Create(resource, action, request.Description);
        await _permissions.AddAsync(permission, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return permission.Id;
    }
}
