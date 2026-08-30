using AccessManagement.Application.Authorization.Audit;
using AccessManagement.Application.Authorization.Services;
using AccessManagement.Application.Common.Exceptions;
using AccessManagement.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AccessManagement.Application.Authorization.Commands.RevokeDelegation;

public record RevokeDelegationCommand(Guid DelegationId, Guid ActorUserId) : IRequest;

public class RevokeDelegationCommandHandler : IRequestHandler<RevokeDelegationCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly IAuthorizationAuditService _audit;
    private readonly IActorAccessService _actorAccess;
    private readonly ICurrentUser _currentUser;

    public RevokeDelegationCommandHandler(
        IApplicationDbContext context,
        IAuthorizationAuditService audit,
        IActorAccessService actorAccess,
        ICurrentUser currentUser)
    {
        _context = context;
        _audit = audit;
        _actorAccess = actorAccess;
        _currentUser = currentUser;
    }

    public async Task Handle(RevokeDelegationCommand request, CancellationToken cancellationToken)
    {
        var delegation = await _context.Delegations
            .FirstOrDefaultAsync(d => d.Id == request.DelegationId, cancellationToken)
            ?? throw new InvalidOperationException($"Delegation {request.DelegationId} not found.");

        var isParty = request.ActorUserId == delegation.DelegatorUserId
                      || request.ActorUserId == delegation.DelegateUserId;
        if (!isParty)
        {
            var isAdmin = await _actorAccess.IsUserAdminAsync(
                request.ActorUserId, _currentUser.ActiveCompanyId, cancellationToken);
            if (!isAdmin)
            {
                throw new ForbiddenAccessException("Only the delegator, delegate, or a user admin may revoke a delegation.");
            }
        }

        delegation.Revoke();
        await _audit.RecordAsync(
            "DelegationRevoked",
            request.ActorUserId,
            $"delegationId={delegation.Id}",
            cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
