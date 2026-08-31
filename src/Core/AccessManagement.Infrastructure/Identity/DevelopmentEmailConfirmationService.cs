using System.Collections.Concurrent;
using AccessManagement.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace AccessManagement.Infrastructure.Identity;

/// <summary>
/// In-memory confirmation tokens. TODO: send via a real email provider.
/// </summary>
public sealed class DevelopmentEmailConfirmationService : IEmailConfirmationService
{
    private readonly ConcurrentDictionary<Guid, string> _tokens = new();
    private readonly ILogger<DevelopmentEmailConfirmationService> _logger;

    public DevelopmentEmailConfirmationService(ILogger<DevelopmentEmailConfirmationService> logger) =>
        _logger = logger;

    public Task<string> CreateTokenAsync(Guid userId, string email, CancellationToken cancellationToken = default)
    {
        var token = Convert.ToHexString(Guid.NewGuid().ToByteArray());
        _tokens[userId] = token;
        _logger.LogInformation("Email confirmation token for {Email} ({UserId}): {Token}", email, userId, token);
        return Task.FromResult(token);
    }

    public Task ConfirmAsync(Guid userId, string token, CancellationToken cancellationToken = default)
    {
        if (!_tokens.TryGetValue(userId, out var expected)
            || !string.Equals(expected, token, StringComparison.Ordinal))
        {
            throw new UnauthorizedAccessException("Invalid credentials.");
        }

        _tokens.TryRemove(userId, out _);
        return Task.CompletedTask;
    }
}
