using qc_authorization.Domain.Authorization;

namespace qc_authorization.Application.Common.Interfaces.Repositories;

public interface IDelegationRepository
{
    Task AddAsync(Delegation delegation, CancellationToken cancellationToken = default);

    Task<Delegation?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Delegation>> GetActiveForDelegateAsync(
        int delegateUserId,
        DateTimeOffset when,
        CancellationToken cancellationToken = default);
}
