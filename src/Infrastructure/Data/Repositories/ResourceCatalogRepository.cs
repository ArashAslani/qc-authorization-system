using qc_authorization.Application.Common.Interfaces.Repositories;
using qc_authorization.Domain.Authorization;
using qc_authorization.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace qc_authorization.Infrastructure.Data.Repositories;

public sealed class ResourceCatalogRepository(ApplicationDbContext context) : IResourceCatalogRepository
{
    public async Task<ResourceCatalog?> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        var normalized = code.ToUpperInvariant();
        return await context.ResourceCatalogs
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Code == normalized, cancellationToken);
    }

    public Task AddAsync(ResourceCatalog resource, CancellationToken cancellationToken = default)
    {
        context.ResourceCatalogs.Add(resource);
        return Task.CompletedTask;
    }
}
