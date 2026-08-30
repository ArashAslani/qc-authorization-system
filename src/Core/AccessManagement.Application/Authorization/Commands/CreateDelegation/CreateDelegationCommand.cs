using AccessManagement.Application.Authorization.Audit;
using AccessManagement.Application.Authorization.Delegation;
using AccessManagement.Application.Common.Interfaces;
using AccessManagement.Domain.Authorization;
using AccessManagement.Domain.Authorization.Exceptions;
using AccessManagement.Domain.Authorization.ValueObjects;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AccessManagement.Application.Authorization.Commands.CreateDelegation;

public record CreateDelegationCommand(
    Guid DelegatorUserId,
    Guid DelegateUserId,
    Guid PermissionId,
    DateTimeOffset ValidFrom,
    DateTimeOffset? ValidTo,
    Guid? ScopeUnitId = null,
    bool Delegable = true,
    Guid? ParentDelegationId = null) : IRequest<Guid>;

public class CreateDelegationCommandHandler : IRequestHandler<CreateDelegationCommand, Guid>
{
    private readonly IApplicationDbContext _context;
    private readonly IDelegationSubsetPolicy _subsetPolicy;
    private readonly IDelegationHierarchyPolicy _hierarchyPolicy;
    private readonly IAuthorizationAuditService _audit;

    public CreateDelegationCommandHandler(
        IApplicationDbContext context,
        IDelegationSubsetPolicy subsetPolicy,
        IDelegationHierarchyPolicy hierarchyPolicy,
        IAuthorizationAuditService audit)
    {
        _context = context;
        _subsetPolicy = subsetPolicy;
        _hierarchyPolicy = hierarchyPolicy;
        _audit = audit;
    }

    public async Task<Guid> Handle(CreateDelegationCommand request, CancellationToken cancellationToken)
    {
        if (request.ParentDelegationId is Guid parentId)
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
            request.ScopeUnitId,
            request.ValidFrom,
            cancellationToken);

        await _hierarchyPolicy.EnsureDelegateeIsSubordinateAsync(
            request.DelegatorUserId,
            request.DelegateUserId,
            request.PermissionId,
            request.ValidFrom,
            cancellationToken);

        var delegation = Domain.Authorization.Delegation.Create(
            request.DelegatorUserId,
            request.DelegateUserId,
            request.PermissionId,
            request.ValidFrom,
            request.ValidTo,
            request.ScopeUnitId,
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
