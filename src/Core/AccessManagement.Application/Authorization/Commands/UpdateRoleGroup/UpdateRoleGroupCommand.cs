using AccessManagement.Application.Authorization.Audit;
using AccessManagement.Application.Common.Interfaces;
using AccessManagement.Domain.Authorization.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AccessManagement.Application.Authorization.Commands.UpdateRoleGroup;

public record UpdateRoleGroupCommand(
    Guid Id,
    string Name,
    string? Description,
    CatalogStatus Status) : IRequest;

public class UpdateRoleGroupCommandHandler : IRequestHandler<UpdateRoleGroupCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly IAuthorizationAuditService _audit;

    public UpdateRoleGroupCommandHandler(IApplicationDbContext context, IAuthorizationAuditService audit)
    {
        _context = context;
        _audit = audit;
    }

    public async Task Handle(UpdateRoleGroupCommand request, CancellationToken cancellationToken)
    {
        var roleGroup = await _context.RoleGroups
            .FirstOrDefaultAsync(rg => rg.Id == request.Id, cancellationToken)
            ?? throw new InvalidOperationException($"RoleGroup {request.Id} not found.");

        var before = new { roleGroup.Name, roleGroup.Description, roleGroup.Status };
        roleGroup.Update(request.Name, request.Description, request.Status);
        var after = new { roleGroup.Name, roleGroup.Description, roleGroup.Status };

        await _audit.RecordChangeAsync("RoleGroupUpdated", null, before, after, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
