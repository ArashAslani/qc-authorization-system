using qc_authorization.Domain.Organization;

namespace qc_authorization.Application.Common.Interfaces.Repositories;

public interface IPersonnelRepository
{
    Task<Personnel?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<Personnel?> GetBySystemUserIdAsync(int systemUserId, CancellationToken cancellationToken = default);

    Task AddAsync(Personnel personnel, CancellationToken cancellationToken = default);
}
