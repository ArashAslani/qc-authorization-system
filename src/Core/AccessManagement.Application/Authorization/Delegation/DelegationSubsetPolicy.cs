using AccessManagement.Application.Authorization.Services;
using AccessManagement.Application.Common.Interfaces;
using AccessManagement.Domain.Authorization.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace AccessManagement.Application.Authorization.Delegation;

public sealed class DelegationSubsetPolicy : IDelegationSubsetPolicy
{
    private readonly IActorAccessService _actorAccess;
    private readonly IApplicationDbContext _context;

    public DelegationSubsetPolicy(
        IActorAccessService actorAccess,
        IApplicationDbContext context)
    {
        _actorAccess = actorAccess;
        _context = context;
    }

    public async Task EnsureDelegatorCanDelegateAsync(
        Guid delegatorUserId,
        Guid permissionId,
        Guid? scopeUnitId,
        DateTimeOffset when,
        Guid? delegatorCompanyUnitId = null,
        CancellationToken cancellationToken = default)
    {
        _ = when;
        var permission = await _context.Permissions
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == permissionId, cancellationToken)
            ?? throw new InvalidOperationException($"Permission {permissionId} not found.");

        var allowed = await _actorAccess.HasPermissionAsync(
            delegatorUserId,
            delegatorCompanyUnitId,
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
