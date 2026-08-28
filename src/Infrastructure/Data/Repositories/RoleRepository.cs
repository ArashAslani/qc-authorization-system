using qc_authorization.Application.Common.Interfaces.Repositories;
using qc_authorization.Domain.Authorization;
using qc_authorization.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace qc_authorization.Infrastructure.Data.Repositories;

public sealed class RoleRepository(ApplicationDbContext context) : IRoleRepository
{
    public async Task<Role?> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        var normalized = code.ToUpperInvariant();
        return await context.Roles
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Code == normalized, cancellationToken);
    }

    public async Task<Role?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        await context.Roles.FindAsync([id], cancellationToken);

    public Task AddAsync(Role role, CancellationToken cancellationToken = default)
    {
        context.Roles.Add(role);
        return Task.CompletedTask;
    }

    public Task AddPermissionAsync(RolePermission rolePermission, CancellationToken cancellationToken = default)
    {
        context.RolePermissions.Add(rolePermission);
        return Task.CompletedTask;
    }
}
