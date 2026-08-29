using qc_authorization.Application.Common.Interfaces;
using qc_authorization.Domain.Authorization;
using qc_authorization.Domain.Authorization.Enums;
using Microsoft.EntityFrameworkCore;

namespace qc_authorization.Application.Authorization.Evaluation;

public interface ICatalogGrantFilter
{
    Task<IReadOnlyList<Grant>> FilterActiveCatalogSourcesAsync(
        IReadOnlyList<Grant> grants,
        CancellationToken cancellationToken = default);
}

public sealed class CatalogGrantFilter : ICatalogGrantFilter
{
    private readonly IApplicationDbContext _context;

    public CatalogGrantFilter(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<Grant>> FilterActiveCatalogSourcesAsync(
        IReadOnlyList<Grant> grants,
        CancellationToken cancellationToken = default)
    {
        if (grants.Count == 0)
        {
            return grants;
        }

        var roleSourceIds = grants
            .Where(g => g.SourceType == SourceType.Role)
            .Select(g => g.SourceId)
            .Distinct()
            .ToList();

        var roleGroupSourceIds = grants
            .Where(g => g.SourceType == SourceType.RoleGroup)
            .Select(g => g.SourceId)
            .Distinct()
            .ToList();

        var inactiveRoleIds = roleSourceIds.Count == 0
            ? new HashSet<Guid>()
            : (await _context.AuthorizationRoles
                .AsNoTracking()
                .Where(r => roleSourceIds.Contains(r.Id) && r.Status != CatalogStatus.Active)
                .Select(r => r.Id)
                .ToListAsync(cancellationToken))
                .ToHashSet();

        var inactiveRoleGroupIds = roleGroupSourceIds.Count == 0
            ? new HashSet<Guid>()
            : (await _context.RoleGroups
                .AsNoTracking()
                .Where(rg => roleGroupSourceIds.Contains(rg.Id) && rg.Status != CatalogStatus.Active)
                .Select(rg => rg.Id)
                .ToListAsync(cancellationToken))
                .ToHashSet();

        return grants
            .Where(g => g.SourceType switch
            {
                SourceType.Role => !inactiveRoleIds.Contains(g.SourceId),
                SourceType.RoleGroup => !inactiveRoleGroupIds.Contains(g.SourceId),
                _ => true,
            })
            .ToList();
    }
}
