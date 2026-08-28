using qc_authorization.Application.Common.Interfaces;
using qc_authorization.Application.Common.Interfaces.Repositories;
using qc_authorization.Domain.Authorization.Audit;

namespace qc_authorization.Application.Authorization.Audit;

public interface IAuthorizationAuditService
{
    Task RecordAsync(string eventType, int? actorUserId, string payload, CancellationToken cancellationToken = default);
}

public sealed class AuthorizationAuditService : IAuthorizationAuditService
{
    private readonly IAuthorizationAuditRepository _audit;

    public AuthorizationAuditService(IAuthorizationAuditRepository audit)
    {
        _audit = audit;
    }

    public Task RecordAsync(string eventType, int? actorUserId, string payload, CancellationToken cancellationToken = default) =>
        _audit.AddAsync(AuthorizationAuditEntry.Create(eventType, actorUserId, payload), cancellationToken);
}
