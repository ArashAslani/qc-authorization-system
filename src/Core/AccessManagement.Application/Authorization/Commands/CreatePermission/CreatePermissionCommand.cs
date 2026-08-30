using AccessManagement.Application.Common.Interfaces;
using AccessManagement.Domain.Authorization;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AccessManagement.Application.Authorization.Commands.CreatePermission;

public record CreatePermissionCommand(
    string ResourceCode,
    string ResourceName,
    string ActionCode,
    string ActionName,
    string? Description = null) : IRequest<Guid>;

public class CreatePermissionCommandHandler : IRequestHandler<CreatePermissionCommand, Guid>
{
    private readonly IApplicationDbContext _context;

    public CreatePermissionCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Guid> Handle(CreatePermissionCommand request, CancellationToken cancellationToken)
    {
        var resourceCode = request.ResourceCode.ToUpperInvariant();
        var resource = await _context.ResourceCatalogs
            .FirstOrDefaultAsync(r => r.Code == resourceCode, cancellationToken)
            ?? ResourceCatalog.Create(request.ResourceCode, request.ResourceName);

        if (resource.Id == Guid.Empty)
        {
            _context.ResourceCatalogs.Add(resource);
            await _context.SaveChangesAsync(cancellationToken);
        }

        var actionCode = request.ActionCode.ToUpperInvariant();
        var action = await _context.ActionCatalogs
            .FirstOrDefaultAsync(a => a.Code == actionCode, cancellationToken)
            ?? ActionCatalog.Create(request.ActionCode, request.ActionName);

        if (action.Id == Guid.Empty)
        {
            _context.ActionCatalogs.Add(action);
            await _context.SaveChangesAsync(cancellationToken);
        }

        var permission = Permission.Create(resource, action, request.Description);
        _context.Permissions.Add(permission);
        await _context.SaveChangesAsync(cancellationToken);
        return permission.Id;
    }
}
