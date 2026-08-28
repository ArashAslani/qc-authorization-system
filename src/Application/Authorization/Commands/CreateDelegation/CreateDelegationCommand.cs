using qc_authorization.Application.Authorization.Audit;
using qc_authorization.Application.Authorization.Delegation;
using qc_authorization.Application.Common.Interfaces;
using qc_authorization.Application.Common.Interfaces.Repositories;
using qc_authorization.Domain.Authorization;
using qc_authorization.Domain.Authorization.Exceptions;
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
    bool Delegable = true,
    int? ParentDelegationId = null) : IRequest<int>;

public class CreateDelegationCommandHandler : IRequestHandler<CreateDelegationCommand, int>
{
    private readonly IDelegationRepository _delegations;
    private readonly IDelegationSubsetPolicy _subsetPolicy;
    private readonly IAuthorizationAuditService _audit;
    private readonly IUnitOfWork _unitOfWork;

    public CreateDelegationCommandHandler(
        IDelegationRepository delegations,
        IDelegationSubsetPolicy subsetPolicy,
        IAuthorizationAuditService audit,
        IUnitOfWork unitOfWork)
    {
        _delegations = delegations;
        _subsetPolicy = subsetPolicy;
        _audit = audit;
        _unitOfWork = unitOfWork;
    }

    public async Task<int> Handle(CreateDelegationCommand request, CancellationToken cancellationToken)
    {
        if (request.ParentDelegationId is int parentId)
        {
            var parent = await _delegations.GetByIdAsync(parentId, cancellationToken)
                ?? throw new InvalidOperationException($"Parent delegation {parentId} not found.");

            if (parent.IsRevoked)
            {
                throw new AuthorizationDomainException("Cannot chain from a revoked delegation.");
            }

            if (!parent.Delegable)
            {
                throw new AuthorizationDomainException("Parent delegation is not delegable.");
            }
        }

        await _subsetPolicy.EnsureDelegatorCanDelegateAsync(
            request.DelegatorUserId,
            request.PermissionId,
            request.ScopeKind,
            request.ScopeIdentifier,
            request.ValidFrom,
            cancellationToken);

        var delegation = Domain.Authorization.Delegation.Create(
            request.DelegatorUserId,
            request.DelegateUserId,
            request.PermissionId,
            request.ValidFrom,
            request.ValidTo,
            request.ScopeKind,
            request.ScopeIdentifier,
            request.Delegable);

        await _delegations.AddAsync(delegation, cancellationToken);
        await _audit.RecordAsync(
            "DelegationCreated",
            request.DelegatorUserId,
            $"delegateUserId={request.DelegateUserId};permissionId={request.PermissionId}",
            cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return delegation.Id;
    }
}
