using qc_authorization.Application.Common.Interfaces.Repositories;
using qc_authorization.Domain.Organization;
using qc_authorization.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace qc_authorization.Infrastructure.Data.Repositories;

public sealed class PositionRepository(ApplicationDbContext context) : IPositionRepository
{
    public async Task<Position?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        await context.Positions.FindAsync([id], cancellationToken);

    public async Task<IReadOnlyList<Position>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await context.Positions.AsNoTracking().ToListAsync(cancellationToken);

    public Task AddAsync(Position position, CancellationToken cancellationToken = default)
    {
        context.Positions.Add(position);
        return Task.CompletedTask;
    }
}
