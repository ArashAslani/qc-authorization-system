using AccessManagement.Application.Authorization.Audit;
using AccessManagement.Application.Authorization.Services;
using AccessManagement.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AccessManagement.Application.Authorization.Commands.AssignRoleGroupToPosition;

public record AssignRoleGroupToPositionCommand(
    Guid PositionId,
    Guid RoleGroupId,
    DateTimeOffset ValidFrom,
    DateTimeOffset? ValidTo = null) : IRequest;

public class AssignRoleGroupToPositionCommandHandler : IRequestHandler<AssignRoleGroupToPositionCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly IAuthorizationAuditService _audit;
    private readonly RoleGroupGrantMaterializer _materializer;

    public AssignRoleGroupToPositionCommandHandler(
        IApplicationDbContext context,
        IAuthorizationAuditService audit,
        RoleGroupGrantMaterializer materializer)
    {
        _context = context;
        _audit = audit;
        _materializer = materializer;
    }

    public async Task Handle(AssignRoleGroupToPositionCommand request, CancellationToken cancellationToken)
    {
        _ = await _context.Positions
            .AsNoTracking()
            .SingleOrDefaultAsync(p => p.Id == request.PositionId, cancellationToken)
            ?? throw new InvalidOperationException($"Position {request.PositionId} not found.");

        var grantCount = await _materializer.MaterializeForPositionAsync(
            request.PositionId,
            request.RoleGroupId,
            request.ValidFrom,
            request.ValidTo,
            cancellationToken);

        await _audit.RecordAsync(
            "RoleGroupAssignedToPosition",
            null,
            $"positionId={request.PositionId};roleGroupId={request.RoleGroupId};grantCount={grantCount}",
            cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
