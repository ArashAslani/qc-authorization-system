using AccessManagement.Application.Authorization.Audit;
using AccessManagement.Application.Common.Interfaces;
using AccessManagement.Domain.Authorization.Enums;
using AccessManagement.Application.Common.Security;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AccessManagement.Application.Authorization.Commands.RevokeRoleGroupFromUser;

public record RevokeRoleGroupFromUserCommand(Guid UserId, Guid RoleGroupId) : IRequest, IRequireUserAdmin;

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
        var now = DateTimeOffset.UtcNow;
        var grants = await _context.Grants
            .Where(g => g.SubjectUserId == request.UserId
                     && g.SourceType == SourceType.RoleGroup
                     && g.SourceId == request.RoleGroupId
                     && (g.ValidTo == null || g.ValidTo > now))
            .ToListAsync(cancellationToken);

        foreach (var grant in grants)
        {
            grant.Deactivate(now);
        }
        await _audit.RecordAsync(
            "RoleGroupRevokedFromUser",
            null,
            $"userId={request.UserId};roleGroupId={request.RoleGroupId};grantCount={grants.Count}",
            cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
