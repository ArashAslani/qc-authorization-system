using qc_authorization.Application.Authorization.Audit;
using qc_authorization.Application.Common.Interfaces;
using qc_authorization.Domain.Authorization.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace qc_authorization.Application.Authorization.Commands.RevokeRoleGroupFromPosition;

public record RevokeRoleGroupFromPositionCommand(Guid PositionId, Guid RoleGroupId) : IRequest;

public class RevokeRoleGroupFromPositionCommandHandler : IRequestHandler<RevokeRoleGroupFromPositionCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly IAuthorizationAuditService _audit;

    public RevokeRoleGroupFromPositionCommandHandler(
        IApplicationDbContext context,
        IAuthorizationAuditService audit)
    {
        _context = context;
        _audit = audit;
    }

    public async Task Handle(RevokeRoleGroupFromPositionCommand request, CancellationToken cancellationToken)
    {
        var grants = await _context.Grants
            .Where(g => g.SubjectType == SubjectType.Position
                     && g.SubjectId == request.PositionId
                     && g.SourceType == SourceType.RoleGroup
                     && g.SourceId == request.RoleGroupId)
            .ToListAsync(cancellationToken);

        _context.Grants.RemoveRange(grants);
        await _audit.RecordAsync(
            "RoleGroupRevokedFromPosition",
            null,
            $"positionId={request.PositionId};roleGroupId={request.RoleGroupId};grantCount={grants.Count}",
            cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
