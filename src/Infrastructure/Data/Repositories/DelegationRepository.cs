using qc_authorization.Application.Common.Interfaces.Repositories;
using qc_authorization.Domain.Authorization;
using qc_authorization.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace qc_authorization.Infrastructure.Data.Repositories;

public sealed class DelegationRepository(ApplicationDbContext context) : IDelegationRepository
{
    public Task AddAsync(Delegation delegation, CancellationToken cancellationToken = default)
    {
        context.Delegations.Add(delegation);
        return Task.CompletedTask;
    }

    public async Task<Delegation?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        await context.Delegations.FindAsync([id], cancellationToken);

    public async Task<IReadOnlyList<Delegation>> GetActiveForDelegateAsync(
        int delegateUserId,
        DateTimeOffset when,
        CancellationToken cancellationToken = default) =>
        await context.Delegations
            .AsNoTracking()
            .Where(d => d.DelegateUserId == delegateUserId
                     && !d.IsRevoked
                     && d.ValidFrom <= when
                     && (d.ValidTo == null || when <= d.ValidTo))
            .ToListAsync(cancellationToken);
}
