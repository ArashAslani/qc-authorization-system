using AccessManagement.Application.Authorization.Audit;
using AccessManagement.Application.Authorization.Delegation;
using AccessManagement.Application.Authorization.Services;
using AccessManagement.Application.Common.Exceptions;
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
    Guid? ParentDelegationId = null,
    Guid? DelegatorCompanyUnitId = null) : IRequest<Guid>;

public class CreateDelegationCommandHandler : IRequestHandler<CreateDelegationCommand, Guid>
{
    private readonly IApplicationDbContext _context;
    private readonly IDelegationSubsetPolicy _subsetPolicy;
    private readonly IDelegationHierarchyPolicy _hierarchyPolicy;
    private readonly IAuthorizationAuditService _audit;
    private readonly ICurrentUser _currentUser;
    private readonly IActorAccessService _actorAccess;

    public CreateDelegationCommandHandler(
        IApplicationDbContext context,
        IDelegationSubsetPolicy subsetPolicy,
        IDelegationHierarchyPolicy hierarchyPolicy,
        IAuthorizationAuditService audit,
        ICurrentUser currentUser,
        IActorAccessService actorAccess)
    {
        _context = context;
        _subsetPolicy = subsetPolicy;
        _hierarchyPolicy = hierarchyPolicy;
        _audit = audit;
        _currentUser = currentUser;
        _actorAccess = actorAccess;
    }

    public async Task<Guid> Handle(CreateDelegationCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is Guid actorUserId && actorUserId != request.DelegatorUserId)
        {
            var isAdmin = await _actorAccess.IsUserAdminAsync(
                actorUserId, request.DelegatorCompanyUnitId ?? _currentUser.ActiveCompanyId, cancellationToken);
            if (!isAdmin)
            {
                throw new ForbiddenAccessException("DelegatorUserId must match the authenticated user.");
            }
        }

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
            request.DelegatorCompanyUnitId,
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
            request.DelegatorUserId,
            $"delegateUserId={request.DelegateUserId};permissionId={request.PermissionId}",
            cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        return delegation.Id;
    }
}
