using qc_authorization.Domain.Authorization.Audit;

namespace qc_authorization.Application.Common.Interfaces.Repositories;

public interface IAuthorizationAuditRepository
{
    Task AddAsync(AuthorizationAuditEntry entry, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AuthorizationAuditEntry>> GetByEventTypeAsync(
        string eventType,
        CancellationToken cancellationToken = default);
}
