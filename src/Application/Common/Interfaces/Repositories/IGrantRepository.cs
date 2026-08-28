using qc_authorization.Domain.Authorization;

namespace qc_authorization.Application.Common.Interfaces.Repositories;

public interface IGrantRepository
{
    Task AddAsync(Grant grant, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Grant>> GetByPermissionAndResourceAsync(
        int permissionId,
        string? resource,
        CancellationToken cancellationToken = default);
}
