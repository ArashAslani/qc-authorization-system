using AccessManagement.Application.Authorization.Audit;
using AccessManagement.Application.Common.Interfaces;
using AccessManagement.Domain.Authorization.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AccessManagement.Application.Authorization.Commands.UpdateRole;

public record UpdateRoleCommand(
    Guid Id,
    string Name,
    string? Description,
    CatalogStatus Status) : IRequest;

public class UpdateRoleCommandHandler : IRequestHandler<UpdateRoleCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly IAuthorizationAuditService _audit;

    public UpdateRoleCommandHandler(IApplicationDbContext context, IAuthorizationAuditService audit)
    {
        _context = context;
        _audit = audit;
    }

    public async Task Handle(UpdateRoleCommand request, CancellationToken cancellationToken)
    {
        var role = await _context.AuthorizationRoles
            .FirstOrDefaultAsync(r => r.Id == request.Id, cancellationToken)
            ?? throw new InvalidOperationException($"Role {request.Id} not found.");

        var before = new { role.Name, role.Description, role.Status };
        role.Update(request.Name, request.Description, request.Status);
        var after = new { role.Name, role.Description, role.Status };

        await _audit.RecordChangeAsync("RoleUpdated", null, before, after, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
