using qc_authorization.Application.Common.Interfaces.Repositories;
using qc_authorization.Domain.Authorization.Audit;
using qc_authorization.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace qc_authorization.Infrastructure.Data.Repositories;

public sealed class AuthorizationAuditRepository(ApplicationDbContext context) : IAuthorizationAuditRepository
{
    public Task AddAsync(AuthorizationAuditEntry entry, CancellationToken cancellationToken = default)
    {
        context.AuthorizationAuditEntries.Add(entry);
        return Task.CompletedTask;
    }

    public async Task<IReadOnlyList<AuthorizationAuditEntry>> GetByEventTypeAsync(
        string eventType,
        CancellationToken cancellationToken = default) =>
        await context.AuthorizationAuditEntries
            .AsNoTracking()
            .Where(x => x.EventType == eventType)
            .OrderBy(x => x.Id)
            .ToListAsync(cancellationToken);
}
