using AccessManagement.Application.Authorization.Audit;
using AccessManagement.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AccessManagement.Application.Authorization.Commands.RevokeGrant;

public record RevokeGrantCommand(Guid GrantId, Guid? ActorUserId = null) : IRequest;

public class RevokeGrantCommandHandler : IRequestHandler<RevokeGrantCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly IAuthorizationAuditService _audit;

    public RevokeGrantCommandHandler(IApplicationDbContext context, IAuthorizationAuditService audit)
    {
        _context = context;
        _audit = audit;
    }

    public async Task Handle(RevokeGrantCommand request, CancellationToken cancellationToken)
    {
        var grant = await _context.Grants
            .FirstOrDefaultAsync(g => g.Id == request.GrantId, cancellationToken)
            ?? throw new InvalidOperationException($"Grant {request.GrantId} not found.");

        grant.Deactivate(DateTimeOffset.UtcNow);
        await _audit.RecordAsync(
            "GrantRevoked",
            request.ActorUserId,
            $"grantId={grant.Id};subject={grant.SubjectType}:{grant.SubjectId}",
            cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
