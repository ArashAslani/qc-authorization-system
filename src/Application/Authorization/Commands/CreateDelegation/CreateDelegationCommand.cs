using qc_authorization.Application.Authorization.Audit;
using qc_authorization.Application.Authorization.Delegation;
using qc_authorization.Application.Common.Interfaces;
using qc_authorization.Domain.Authorization;
using qc_authorization.Domain.Authorization.Exceptions;
using qc_authorization.Domain.Authorization.ValueObjects;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace qc_authorization.Application.Authorization.Commands.CreateDelegation;

public record CreateDelegationCommand(
    Guid DelegatorUserId,
    Guid DelegateUserId,
    int PermissionId,
    DateTimeOffset ValidFrom,
    DateTimeOffset? ValidTo,
    ScopeKind ScopeKind = ScopeKind.Unbounded,
    string? ScopeIdentifier = null,
    bool Delegable = true,
    int? ParentDelegationId = null) : IRequest<int>;

public class CreateDelegationCommandHandler : IRequestHandler<CreateDelegationCommand, int>
{
    private readonly IApplicationDbContext _context;
    private readonly IDelegationSubsetPolicy _subsetPolicy;
    private readonly IAuthorizationAuditService _audit;

    public CreateDelegationCommandHandler(
        IApplicationDbContext context,
        IDelegationSubsetPolicy subsetPolicy,
        IAuthorizationAuditService audit)
    {
        _context = context;
        _subsetPolicy = subsetPolicy;
        _audit = audit;
    }

    public async Task<int> Handle(CreateDelegationCommand request, CancellationToken cancellationToken)
    {
        if (request.ParentDelegationId is int parentId)
        {
            var parent = await _context.Delegations
                .FirstOrDefaultAsync(d => d.Id == parentId, cancellationToken)
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

        _context.Delegations.Add(delegation);
        await _audit.RecordAsync(
            "DelegationCreated",
            null,
            $"delegateUserId={request.DelegateUserId};permissionId={request.PermissionId}",
            cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        return delegation.Id;
    }
}
