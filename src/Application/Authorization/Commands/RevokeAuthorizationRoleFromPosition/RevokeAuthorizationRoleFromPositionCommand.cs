using qc_authorization.Application.Authorization.Audit;
using qc_authorization.Application.Common.Interfaces;
using qc_authorization.Domain.Authorization.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace qc_authorization.Application.Authorization.Commands.RevokeAuthorizationRoleFromPosition;

public record RevokeAuthorizationRoleFromPositionCommand(Guid PositionId, Guid RoleId) : IRequest;

public class RevokeAuthorizationRoleFromPositionCommandHandler : IRequestHandler<RevokeAuthorizationRoleFromPositionCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly IAuthorizationAuditService _audit;

    public RevokeAuthorizationRoleFromPositionCommandHandler(
        IApplicationDbContext context,
        IAuthorizationAuditService audit)
    {
        _context = context;
        _audit = audit;
    }

    public async Task Handle(RevokeAuthorizationRoleFromPositionCommand request, CancellationToken cancellationToken)
    {
        var grants = await _context.Grants
            .Where(g => g.SubjectType == SubjectType.Position
                     && g.SubjectId == request.PositionId
                     && g.SourceType == SourceType.Role
                     && g.SourceId == request.RoleId)
            .ToListAsync(cancellationToken);

        _context.Grants.RemoveRange(grants);
        await _audit.RecordChangeAsync(
            "RoleRevokedFromPosition",
            null,
            new { request.PositionId, request.RoleId, grantCount = grants.Count },
            null,
            cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
