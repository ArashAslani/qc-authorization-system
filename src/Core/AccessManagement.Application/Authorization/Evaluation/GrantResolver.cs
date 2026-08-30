using AccessManagement.Application.Abstractions;
using AccessManagement.Application.Common.Interfaces;
using AccessManagement.Domain.Authorization;
using AccessManagement.Domain.Authorization.Enums;
using AccessManagement.Domain.Authorization.Evaluation;
using AccessManagement.Domain.Authorization.Services;
using Microsoft.EntityFrameworkCore;

namespace AccessManagement.Application.Authorization.Evaluation;

/// <summary>
/// Resolves candidate grants. Allow on a position is visible to ancestors
/// of that position (computed). Individual grants never propagate.
/// </summary>
public sealed class GrantResolver : IGrantResolver, ICandidateGrantResolver
{
    private readonly IApplicationDbContext _context;
    private readonly GrantApplicabilityService _applicability;
    private readonly ICatalogGrantFilter _catalogFilter;

    public GrantResolver(
        IApplicationDbContext context,
        GrantApplicabilityService applicability,
        ICatalogGrantFilter catalogFilter)
    {
        _context = context;
        _applicability = applicability;
        _catalogFilter = catalogFilter;
    }

    public Task<IReadOnlyList<Grant>> ResolveAsync(AccessRequest request, CancellationToken cancellationToken) =>
        FindCandidatesAsync(request, cancellationToken);

    public async Task<IReadOnlyList<Grant>> FindCandidatesAsync(AccessRequest request, CancellationToken ct = default)
    {
        var permission = await _context.Permissions
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Code.ToUpper() == request.NormalizedPermissionCode, ct);

        if (permission is null)
        {
            return Array.Empty<Grant>();
        }

        var allGrants = await _context.Grants
            .AsNoTracking()
            .Include(g => g.Constraints)
            .Where(g => g.PermissionId == permission.Id)
            .ToListAsync(ct);

        allGrants = (await _catalogFilter.FilterActiveCatalogSourcesAsync(allGrants, ct)).ToList();

        var allPositions = await _context.Positions.AsNoTracking().ToListAsync(ct);
        var requestPositionIds = request.ActivePositionId is Guid pid
            ? new HashSet<Guid> { pid }
            : new HashSet<Guid>();

        var result = new List<Grant>();
        foreach (var grant in allGrants)
        {
            if (_applicability.Applies(
                    grant,
                    SubjectType.User,
                    request.ActivePositionId ?? Guid.Empty,
                    request.SubjectUserId,
                    requestPositionIds,
                    allPositions))
            {
                result.Add(grant);
            }
        }

        var activeDelegations = await _context.Delegations
            .AsNoTracking()
            .Where(d => d.DelegateUserId == request.SubjectUserId
                     && !d.IsRevoked
                     && d.ValidFrom <= request.When
                     && (d.ValidTo == null || request.When <= d.ValidTo))
            .ToListAsync(ct);

        foreach (var delegation in activeDelegations)
        {
            if (delegation.PermissionId != permission.Id)
            {
                continue;
            }

            var delegatedGrant = delegation.ToGrant();
            if (_applicability.Applies(
                    delegatedGrant,
                    SubjectType.User,
                    Guid.Empty,
                    request.SubjectUserId,
                    requestPositionIds,
                    allPositions))
            {
                result.Add(delegatedGrant);
            }
        }

        return result;
    }
}
