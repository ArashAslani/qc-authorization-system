using qc_authorization.Application.Common.Interfaces.Repositories;
using qc_authorization.Domain.Authorization;
using qc_authorization.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace qc_authorization.Infrastructure.Data.Repositories;

public sealed class PermissionRepository(ApplicationDbContext context) : IPermissionRepository
{
    public async Task<Permission?> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        var normalized = code.ToUpperInvariant();
        return await context.Permissions
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Code.ToUpper() == normalized, cancellationToken);
    }

    public async Task<Permission?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        await context.Permissions.FindAsync([id], cancellationToken);

    public Task AddAsync(Permission permission, CancellationToken cancellationToken = default)
    {
        context.Permissions.Add(permission);
        return Task.CompletedTask;
    }
}
