using qc_authorization.Application.Common.Interfaces;
using qc_authorization.Domain.Authorization.Audit;

namespace qc_authorization.Application.Authorization.Audit;

public interface IAuthorizationAuditService
{
    Task RecordAsync(string eventType, Guid? actorUserId, string payload, CancellationToken cancellationToken = default);
}

public sealed class AuthorizationAuditService : IAuthorizationAuditService
{
    private readonly IApplicationDbContext _context;

    public AuthorizationAuditService(IApplicationDbContext context)
    {
        _context = context;
    }

    public Task RecordAsync(string eventType, Guid? actorUserId, string payload, CancellationToken cancellationToken = default)
    {
        _context.AuthorizationAuditEntries.Add(AuthorizationAuditEntry.Create(eventType, actorUserId, payload));
        return Task.CompletedTask;
    }
}
