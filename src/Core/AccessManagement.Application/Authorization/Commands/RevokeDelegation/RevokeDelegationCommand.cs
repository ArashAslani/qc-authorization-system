using AccessManagement.Application.Authorization.Audit;
using AccessManagement.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AccessManagement.Application.Authorization.Commands.RevokeDelegation;

public record RevokeDelegationCommand(Guid DelegationId) : IRequest;

public class RevokeDelegationCommandHandler : IRequestHandler<RevokeDelegationCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly IAuthorizationAuditService _audit;

    public RevokeDelegationCommandHandler(IApplicationDbContext context, IAuthorizationAuditService audit)
    {
        _context = context;
        _audit = audit;
    }

    public async Task Handle(RevokeDelegationCommand request, CancellationToken cancellationToken)
    {
        var delegation = await _context.Delegations
            .FirstOrDefaultAsync(d => d.Id == request.DelegationId, cancellationToken)
            ?? throw new InvalidOperationException($"Delegation {request.DelegationId} not found.");

        delegation.Revoke();
        await _audit.RecordAsync(
            "DelegationRevoked",
            null,
            $"delegationId={delegation.Id}",
            cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
