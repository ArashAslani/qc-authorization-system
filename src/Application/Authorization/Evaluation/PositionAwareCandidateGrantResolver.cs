using qc_authorization.Application.Common.Interfaces;
using qc_authorization.Domain.Authorization;
using qc_authorization.Domain.Authorization.Enums;
using qc_authorization.Domain.Authorization.Evaluation;
using qc_authorization.Domain.Authorization.Services;
using qc_authorization.Domain.Organization;
using Microsoft.EntityFrameworkCore;

namespace qc_authorization.Application.Authorization.Evaluation;

/// <summary>
/// Resolves candidate grants using EF Core and domain propagation rules.
/// Position grants are scoped to the active company workspace.
/// </summary>
public sealed class PositionAwareCandidateGrantResolver : ICandidateGrantResolver
{
    private readonly IApplicationDbContext _context;
    private readonly GrantApplicabilityService _applicability;
    private readonly ICurrentUser _currentUser;

    public PositionAwareCandidateGrantResolver(
        IApplicationDbContext context,
        GrantApplicabilityService applicability,
        ICurrentUser currentUser)
    {
        _context = context;
        _applicability = applicability;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyList<Grant>> ResolveAsync(AccessRequest request, CancellationToken cancellationToken)
    {
        var normalizedCode = request.NormalizedPermissionCode;
        var permission = await _context.Permissions
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Code.ToUpper() == normalizedCode, cancellationToken);

        if (permission is null)
        {
            return Array.Empty<Grant>();
        }

        var allGrants = await _context.Grants
            .AsNoTracking()
            .Include(g => g.Constraints)
            .Where(g => g.PermissionId == permission.Id
                     && (g.Resource == null || g.Resource == request.Resource))
            .ToListAsync(cancellationToken);

        var allPositions = await _context.Positions.AsNoTracking().ToListAsync(cancellationToken);
        var requestPositionIds = await ResolveRequestPositions(request, cancellationToken);

        var result = new List<Grant>();

        foreach (var grant in allGrants)
        {
            if (_applicability.Applies(
                    grant,
                    request.SubjectType,
                    request.SubjectId,
                    request.UserId,
                    requestPositionIds,
                    allPositions))
            {
                result.Add(grant);
            }
        }

        if (request.SubjectType == SubjectType.User && request.UserId is Guid userId)
        {
            var activeDelegations = await _context.Delegations
                .AsNoTracking()
                .Where(d => d.DelegateUserId == userId
                         && !d.IsRevoked
                         && d.ValidFrom <= request.When
                         && (d.ValidTo == null || request.When <= d.ValidTo))
                .ToListAsync(cancellationToken);

            foreach (var delegation in activeDelegations)
            {
                if (delegation.PermissionId != permission.Id)
                {
                    continue;
                }

                var delegatedGrant = delegation.ToGrant();
                if (_applicability.Applies(
                        delegatedGrant,
                        request.SubjectType,
                        request.SubjectId,
                        request.UserId,
                        requestPositionIds,
                        allPositions))
                {
                    result.Add(delegatedGrant);
                }
            }
        }

        return result;
    }

    private async Task<HashSet<Guid>> ResolveRequestPositions(AccessRequest request, CancellationToken ct)
    {
        if (request.SubjectType == SubjectType.Position)
        {
            return [request.SubjectId];
        }

        if (request.SubjectType != SubjectType.User || request.UserId is not Guid userId)
        {
            return [];
        }

        var activeCompanyId = ResolveActiveCompanyId(request);
        if (!activeCompanyId.HasValue)
        {
            return [];
        }

        var positionIds = await _context.PositionAssignments
            .AsNoTracking()
            .Include(a => a.Position)
            .Where(a => a.Personnel.IdentityUserId == userId
                     && a.Position.CompanyId == activeCompanyId.Value
                     && a.ValidFrom <= request.When
                     && (a.ValidTo == null || request.When <= a.ValidTo))
            .Select(a => a.PositionId)
            .ToListAsync(ct);

        return new HashSet<Guid>(positionIds);
    }

    private Guid? ResolveActiveCompanyId(AccessRequest request)
    {
        if (request.Context is not null
            && request.Context.TryGetValue("CompanyId", out var companyId)
            && companyId is not null)
        {
            return companyId switch
            {
                Guid g => g,
                _ when Guid.TryParse(companyId.ToString(), out var parsed) => parsed,
                _ => null,
            };
        }

        return _currentUser.ActiveCompanyId;
    }
}
