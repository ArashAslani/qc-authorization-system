using AccessManagement.Application.Abstractions;
using AccessManagement.Application.Authorization.Services;
using AccessManagement.Application.Common.Interfaces;
using AccessManagement.Domain.Authorization.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace AccessManagement.Application.Authorization.Delegation;

public sealed class DelegationSubsetPolicy : IDelegationSubsetPolicy
{
    private readonly IActorAccessService _actorAccess;
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUser _currentUser;

    public DelegationSubsetPolicy(
        IActorAccessService actorAccess,
        IApplicationDbContext context,
        ICurrentUser currentUser)
    {
        _actorAccess = actorAccess;
        _context = context;
        _currentUser = currentUser;
    }

    public async Task EnsureDelegatorCanDelegateAsync(
        Guid delegatorUserId,
        Guid permissionId,
        Guid? scopeUnitId,
        DateTimeOffset when,
        CancellationToken cancellationToken = default)
    {
        var permission = await _context.Permissions
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == permissionId, cancellationToken)
            ?? throw new InvalidOperationException($"Permission {permissionId} not found.");

        var allowed = await _actorAccess.HasPermissionAsync(
            delegatorUserId,
            _currentUser.ActiveCompanyId,
            permission.Code,
            scopeUnitId,
            cancellationToken);
        if (!allowed)
        {
            throw new AuthorizationDomainException(
                "Delegator does not have effective access required to delegate this permission.");
        }
    }
}
