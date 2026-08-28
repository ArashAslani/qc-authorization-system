using qc_authorization.Application.Common.Interfaces.Repositories;
using qc_authorization.Domain.Authorization;
using qc_authorization.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace qc_authorization.Infrastructure.Data.Repositories;

public sealed class RoleGroupRepository(ApplicationDbContext context) : IRoleGroupRepository
{
    public async Task<RoleGroup?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        await context.RoleGroups
            .Include(g => g.Members)
            .FirstOrDefaultAsync(g => g.Id == id, cancellationToken);

    public async Task<RoleGroup?> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        var normalized = code.ToUpperInvariant();
        return await context.RoleGroups
            .Include(g => g.Members)
            .FirstOrDefaultAsync(g => g.Code == normalized, cancellationToken);
    }

    public Task AddAsync(RoleGroup roleGroup, CancellationToken cancellationToken = default)
    {
        context.RoleGroups.Add(roleGroup);
        return Task.CompletedTask;
    }
}
