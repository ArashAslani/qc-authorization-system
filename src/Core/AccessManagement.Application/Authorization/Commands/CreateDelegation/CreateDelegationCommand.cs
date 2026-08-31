using AccessManagement.Application.Abstractions;
using AccessManagement.Application.Authorization.Audit;
using AccessManagement.Application.Authorization.Delegation;
using AccessManagement.Application.Authorization.Services;
using AccessManagement.Application.Common.Exceptions;
using AccessManagement.Application.Common.Interfaces;
using AccessManagement.Domain.Authorization.Exceptions;
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
    bool Delegable = false,
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
    private readonly IOrganizationalUnitHierarchy _units;

    public CreateDelegationCommandHandler(
        IApplicationDbContext context,
        IDelegationSubsetPolicy subsetPolicy,
        IDelegationHierarchyPolicy hierarchyPolicy,
        IAuthorizationAuditService audit,
        ICurrentUser currentUser,
        IActorAccessService actorAccess,
        IOrganizationalUnitHierarchy units)
    {
        _context = context;
        _subsetPolicy = subsetPolicy;
        _hierarchyPolicy = hierarchyPolicy;
        _audit = audit;
        _currentUser = currentUser;
        _actorAccess = actorAccess;
        _units = units;
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

        DateTimeOffset? parentValidTo = null;
        Guid? parentScope = null;
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

            parentValidTo = parent.ValidTo;
            parentScope = parent.ScopeUnitId;
            await EnsureChildScopeIsSubsetAsync(parentScope, request.ScopeUnitId, cancellationToken);
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

        var cap = await _subsetPolicy.ResolveDelegatorAccessExpiryAsync(
            request.DelegatorUserId,
            request.PermissionId,
            request.ScopeUnitId,
            request.ValidFrom,
            request.DelegatorCompanyUnitId,
            cancellationToken);

        var validTo = MinExpiry(request.ValidTo, cap, parentValidTo);
        if (validTo is DateTimeOffset expiry && expiry < request.ValidFrom)
        {
            throw new AuthorizationDomainException(
                "Delegation validity exceeds the remaining validity of the delegator's access.");
        }

        var delegation = Domain.Authorization.Delegation.Create(
            request.DelegatorUserId,
            request.DelegateUserId,
            request.PermissionId,
            request.ValidFrom,
            validTo,
            request.ScopeUnitId,
            request.Delegable,
            request.ParentDelegationId);

        _context.Delegations.Add(delegation);
        await _audit.RecordAsync(
            "DelegationCreated",
            request.DelegatorUserId,
            $"delegateUserId={request.DelegateUserId};permissionId={request.PermissionId}",
            cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        return delegation.Id;
    }

    private async Task EnsureChildScopeIsSubsetAsync(
        Guid? parentScope,
        Guid? childScope,
        CancellationToken cancellationToken)
    {
        if (parentScope is null)
        {
            return;
        }

        if (childScope is null)
        {
            throw new AuthorizationDomainException(
                "Child delegation cannot be unbounded when the parent delegation is scoped.");
        }

        if (childScope == parentScope)
        {
            return;
        }

        var descendants = await _units.GetDescendantIdsAsync(parentScope.Value, cancellationToken);
        if (!descendants.Contains(childScope.Value))
        {
            throw new AuthorizationDomainException(
                "Child delegation scope must be a subset of the parent delegation scope.");
        }
    }

    private static DateTimeOffset? MinExpiry(params DateTimeOffset?[] values)
    {
        DateTimeOffset? min = null;
        foreach (var value in values)
        {
            if (value is null)
            {
                continue;
            }

            min = min is null || value < min ? value : min;
        }

        return min;
    }
}
