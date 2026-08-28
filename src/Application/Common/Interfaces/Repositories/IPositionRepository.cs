using qc_authorization.Domain.Organization;

namespace qc_authorization.Application.Common.Interfaces.Repositories;

public interface IPositionRepository
{
    Task<Position?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Position>> GetAllAsync(CancellationToken cancellationToken = default);

    Task AddAsync(Position position, CancellationToken cancellationToken = default);
}
