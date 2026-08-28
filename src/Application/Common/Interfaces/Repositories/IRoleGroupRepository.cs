using qc_authorization.Domain.Authorization;

namespace qc_authorization.Application.Common.Interfaces.Repositories;

public interface IRoleGroupRepository
{
    Task<RoleGroup?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<RoleGroup?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);
    Task AddAsync(RoleGroup roleGroup, CancellationToken cancellationToken = default);
}
