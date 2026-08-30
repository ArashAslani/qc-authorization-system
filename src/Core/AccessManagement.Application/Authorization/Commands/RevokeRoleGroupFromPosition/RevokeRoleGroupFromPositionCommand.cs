using AccessManagement.Application.Authorization.Audit;
using AccessManagement.Application.Common.Interfaces;
using AccessManagement.Domain.Authorization.Enums;
using AccessManagement.Application.Common.Security;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AccessManagement.Application.Authorization.Commands.RevokeRoleGroupFromPosition;

public record RevokeRoleGroupFromPositionCommand(Guid PositionId, Guid RoleGroupId) : IRequest, IRequireUserAdmin;

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
        var now = DateTimeOffset.UtcNow;
        var grants = await _context.Grants
            .Where(g => g.SubjectType == SubjectType.Position
                     && g.SubjectId == request.PositionId
                     && g.SourceType == SourceType.RoleGroup
                     && g.SourceId == request.RoleGroupId
                     && (g.ValidTo == null || g.ValidTo > now))
            .ToListAsync(cancellationToken);

        foreach (var grant in grants)
        {
            grant.Deactivate(now);
        }
        await _audit.RecordAsync(
            "RoleGroupRevokedFromPosition",
            null,
            $"positionId={request.PositionId};roleGroupId={request.RoleGroupId};grantCount={grants.Count}",
            cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
