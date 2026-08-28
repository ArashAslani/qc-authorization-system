using qc_authorization.Application.Common.Interfaces.Repositories;
using qc_authorization.Domain.Organization;
using qc_authorization.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace qc_authorization.Infrastructure.Data.Repositories;

public sealed class PersonnelRepository(ApplicationDbContext context) : IPersonnelRepository
{
    public async Task<Personnel?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        await context.Personnel.FindAsync([id], cancellationToken);

    public async Task<Personnel?> GetBySystemUserIdAsync(int systemUserId, CancellationToken cancellationToken = default) =>
        await context.Personnel
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.SystemUserId == systemUserId, cancellationToken);

    public Task AddAsync(Personnel personnel, CancellationToken cancellationToken = default)
    {
        context.Personnel.Add(personnel);
        return Task.CompletedTask;
    }
}
