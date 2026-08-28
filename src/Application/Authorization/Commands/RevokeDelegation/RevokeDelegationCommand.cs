using qc_authorization.Application.Authorization.Audit;
using qc_authorization.Application.Common.Interfaces;
using qc_authorization.Application.Common.Interfaces.Repositories;
using MediatR;

namespace qc_authorization.Application.Authorization.Commands.RevokeDelegation;

public record RevokeDelegationCommand(int DelegationId) : IRequest;

public class RevokeDelegationCommandHandler : IRequestHandler<RevokeDelegationCommand>
{
    private readonly IDelegationRepository _delegations;
    private readonly IAuthorizationAuditService _audit;
    private readonly IUnitOfWork _unitOfWork;

    public RevokeDelegationCommandHandler(
        IDelegationRepository delegations,
        IAuthorizationAuditService audit,
        IUnitOfWork unitOfWork)
    {
        _delegations = delegations;
        _audit = audit;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(RevokeDelegationCommand request, CancellationToken cancellationToken)
    {
        var delegation = await _delegations.GetByIdAsync(request.DelegationId, cancellationToken)
            ?? throw new InvalidOperationException($"Delegation {request.DelegationId} not found.");

        delegation.Revoke();
        await _audit.RecordAsync(
            "DelegationRevoked",
            delegation.DelegatorUserId,
            $"delegationId={delegation.Id}",
            cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
