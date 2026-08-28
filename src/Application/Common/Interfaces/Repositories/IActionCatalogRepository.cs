using qc_authorization.Domain.Authorization;

namespace qc_authorization.Application.Common.Interfaces.Repositories;

public interface IActionCatalogRepository
{
    Task<ActionCatalog?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);
    Task AddAsync(ActionCatalog action, CancellationToken cancellationToken = default);
}
