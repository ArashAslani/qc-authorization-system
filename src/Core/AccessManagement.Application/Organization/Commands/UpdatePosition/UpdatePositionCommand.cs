using AccessManagement.Application.Authorization.Audit;
using AccessManagement.Application.Common.Interfaces;
using AccessManagement.Domain.Organization.Enums;
using AccessManagement.Application.Common.Security;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AccessManagement.Application.Organization.Commands.UpdatePosition;

public record UpdatePositionCommand(
    Guid Id,
    string Title,
    string? Description,
    PositionStatus Status) : IRequest, IRequireUserAdmin;

public class UpdatePositionCommandHandler : IRequestHandler<UpdatePositionCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly IAuthorizationAuditService _audit;

    public UpdatePositionCommandHandler(IApplicationDbContext context, IAuthorizationAuditService audit)
    {
        _context = context;
        _audit = audit;
    }

    public async Task Handle(UpdatePositionCommand request, CancellationToken cancellationToken)
    {
        var position = await _context.Positions
            .FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken)
            ?? throw new InvalidOperationException($"Position {request.Id} not found.");

        var before = new { position.Title, position.Description, position.Status };
        position.Update(request.Title, request.Description, request.Status);
        var after = new { position.Title, position.Description, position.Status };

        await _audit.RecordChangeAsync("PositionUpdated", null, before, after, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
