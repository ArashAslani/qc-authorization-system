using AccessManagement.Application.Authorization.Audit;
using AccessManagement.Application.Common.Interfaces;
using AccessManagement.Domain.Authorization.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AccessManagement.Application.Authorization.Commands.RevokeAuthorizationRoleFromUser;

public record RevokeAuthorizationRoleFromUserCommand(Guid UserId, Guid RoleId) : IRequest;

public class RevokeAuthorizationRoleFromUserCommandHandler : IRequestHandler<RevokeAuthorizationRoleFromUserCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly IAuthorizationAuditService _audit;

    public RevokeAuthorizationRoleFromUserCommandHandler(
        IApplicationDbContext context,
        IAuthorizationAuditService audit)
    {
        _context = context;
        _audit = audit;
    }

    public async Task Handle(RevokeAuthorizationRoleFromUserCommand request, CancellationToken cancellationToken)
    {
        var grants = await _context.Grants
            .Where(g => g.SubjectUserId == request.UserId
                     && g.SourceType == SourceType.Role
                     && g.SourceId == request.RoleId)
            .ToListAsync(cancellationToken);

        _context.Grants.RemoveRange(grants);
        await _audit.RecordAsync(
            "RoleRevokedFromUser",
            null,
            $"userId={request.UserId};roleId={request.RoleId};grantCount={grants.Count}",
            cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
