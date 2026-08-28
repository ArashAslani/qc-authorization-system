using qc_authorization.Application.Common.Interfaces;
using qc_authorization.Application.Common.Interfaces.Repositories;
using qc_authorization.Domain.Authorization;
using qc_authorization.Domain.Authorization.ValueObjects;
using MediatR;

namespace qc_authorization.Application.Authorization.Commands.CreateDelegation;

public record CreateDelegationCommand(
    int DelegatorUserId,
    int DelegateUserId,
    int PermissionId,
    DateTimeOffset ValidFrom,
    DateTimeOffset? ValidTo,
    ScopeKind ScopeKind = ScopeKind.Unbounded,
    string? ScopeIdentifier = null,
    bool Delegable = true) : IRequest<int>;

public class CreateDelegationCommandHandler : IRequestHandler<CreateDelegationCommand, int>
{
    private readonly IDelegationRepository _delegations;
    private readonly IUnitOfWork _unitOfWork;

    public CreateDelegationCommandHandler(
        IDelegationRepository delegations,
        IUnitOfWork unitOfWork)
    {
        _delegations = delegations;
        _unitOfWork = unitOfWork;
    }

    public async Task<int> Handle(CreateDelegationCommand request, CancellationToken cancellationToken)
    {
        var delegation = Delegation.Create(
            request.DelegatorUserId,
            request.DelegateUserId,
            request.PermissionId,
            request.ValidFrom,
            request.ValidTo,
            request.ScopeKind,
            request.ScopeIdentifier,
            request.Delegable);

        await _delegations.AddAsync(delegation, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return delegation.Id;
    }
}
