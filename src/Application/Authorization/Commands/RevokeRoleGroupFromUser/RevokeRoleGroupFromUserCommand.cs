using qc_authorization.Application.Authorization.Audit;
using qc_authorization.Application.Common.Interfaces;
using qc_authorization.Domain.Authorization.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace qc_authorization.Application.Authorization.Commands.RevokeRoleGroupFromUser;

public record RevokeRoleGroupFromUserCommand(Guid UserId, Guid RoleGroupId) : IRequest;

public class RevokeRoleGroupFromUserCommandHandler : IRequestHandler<RevokeRoleGroupFromUserCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly IAuthorizationAuditService _audit;

    public RevokeRoleGroupFromUserCommandHandler(
        IApplicationDbContext context,
        IAuthorizationAuditService audit)
    {
        _context = context;
        _audit = audit;
    }

    public async Task Handle(RevokeRoleGroupFromUserCommand request, CancellationToken cancellationToken)
    {
        var grants = await _context.Grants
            .Where(g => g.SubjectUserId == request.UserId
                     && g.SourceType == SourceType.RoleGroup
                     && g.SourceId == request.RoleGroupId)
            .ToListAsync(cancellationToken);

        _context.Grants.RemoveRange(grants);
        await _audit.RecordAsync(
            "RoleGroupRevokedFromUser",
            null,
            $"userId={request.UserId};roleGroupId={request.RoleGroupId};grantCount={grants.Count}",
            cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
