using System.Collections.Concurrent;
using AccessManagement.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace AccessManagement.Infrastructure.Identity;

/// <summary>
/// In-memory 6-digit challenge codes. TODO: replace with TOTP authenticator enrollment.
/// </summary>
public sealed class DevelopmentTwoFactorChallengeService : ITwoFactorChallengeService
{
    private readonly ConcurrentDictionary<Guid, string> _codes = new();
    private readonly ILogger<DevelopmentTwoFactorChallengeService> _logger;

    public DevelopmentTwoFactorChallengeService(ILogger<DevelopmentTwoFactorChallengeService> logger) =>
        _logger = logger;

    public Task<string> CreateChallengeCodeAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var code = Random.Shared.Next(0, 1_000_000).ToString("D6");
        _codes[userId] = code;
        _logger.LogInformation("Two-factor challenge code for {UserId}: {Code}", userId, code);
        return Task.FromResult(code);
    }

    public Task<bool> VerifyAsync(Guid userId, string code, CancellationToken cancellationToken = default)
    {
        if (!_codes.TryGetValue(userId, out var expected)
            || !string.Equals(expected, code, StringComparison.Ordinal))
        {
            return Task.FromResult(false);
        }

        _codes.TryRemove(userId, out _);
        return Task.FromResult(true);
    }
}
