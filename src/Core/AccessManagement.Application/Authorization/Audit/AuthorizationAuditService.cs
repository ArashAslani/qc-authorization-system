using System.Text.Json;
using AccessManagement.Application.Common.Interfaces;
using AccessManagement.Domain.Authorization.Audit;

namespace AccessManagement.Application.Authorization.Audit;

public interface IAuthorizationAuditService
{
    Task RecordAsync(string eventType, Guid? actorUserId, string payload, CancellationToken cancellationToken = default);

    Task RecordChangeAsync(
        string eventType,
        Guid? actorUserId,
        object? before,
        object? after,
        CancellationToken cancellationToken = default);
}

public sealed class AuthorizationAuditService : IAuthorizationAuditService
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = false };

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

    public Task RecordChangeAsync(
        string eventType,
        Guid? actorUserId,
        object? before,
        object? after,
        CancellationToken cancellationToken = default)
    {
        var payload = JsonSerializer.Serialize(new { before, after }, JsonOptions);
        _context.AuthorizationAuditEntries.Add(AuthorizationAuditEntry.Create(eventType, actorUserId, payload));
        return Task.CompletedTask;
    }
}
