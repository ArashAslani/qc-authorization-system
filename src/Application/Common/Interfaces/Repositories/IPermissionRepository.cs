using qc_authorization.Domain.Authorization;

namespace qc_authorization.Application.Common.Interfaces.Repositories;

public interface IPermissionRepository
{
    Task<Permission?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);

    Task AddAsync(Permission permission, CancellationToken cancellationToken = default);
}
