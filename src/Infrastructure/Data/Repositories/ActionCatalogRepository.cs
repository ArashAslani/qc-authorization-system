using qc_authorization.Application.Common.Interfaces.Repositories;
using qc_authorization.Domain.Authorization;
using qc_authorization.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace qc_authorization.Infrastructure.Data.Repositories;

public sealed class ActionCatalogRepository(ApplicationDbContext context) : IActionCatalogRepository
{
    public async Task<ActionCatalog?> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        var normalized = code.ToUpperInvariant();
        return await context.ActionCatalogs
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Code == normalized, cancellationToken);
    }

    public Task AddAsync(ActionCatalog action, CancellationToken cancellationToken = default)
    {
        context.ActionCatalogs.Add(action);
        return Task.CompletedTask;
    }
}
