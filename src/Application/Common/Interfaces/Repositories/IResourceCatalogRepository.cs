using qc_authorization.Domain.Authorization;

namespace qc_authorization.Application.Common.Interfaces.Repositories;

public interface IResourceCatalogRepository
{
    Task<ResourceCatalog?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);
    Task AddAsync(ResourceCatalog resource, CancellationToken cancellationToken = default);
}
