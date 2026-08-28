using qc_authorization.Application.Common.Interfaces;
using qc_authorization.Application.Common.Interfaces.Repositories;
using qc_authorization.Domain.Authorization;
using qc_authorization.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace qc_authorization.Infrastructure.Data.Repositories;

public sealed class GrantRepository(ApplicationDbContext context) : IGrantRepository
{
    public Task AddAsync(Grant grant, CancellationToken cancellationToken = default)
    {
        context.Grants.Add(grant);
        return Task.CompletedTask;
    }

    public async Task<IReadOnlyList<Grant>> GetByPermissionAndResourceAsync(
        int permissionId,
        string? resource,
        CancellationToken cancellationToken = default) =>
        await context.Grants
            .AsNoTracking()
            .Where(g => g.PermissionId == permissionId
                     && (g.Resource == null || g.Resource == resource))
            .ToListAsync(cancellationToken);
}
