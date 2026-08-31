namespace AccessManagement.Application.Common.Interfaces;

/// <summary>
/// Simulated two-factor challenge after password verification.
/// TODO: replace with TOTP authenticator enrollment and verification.
/// </summary>
public interface ITwoFactorChallengeService
{
    Task<string> CreateChallengeCodeAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<bool> VerifyAsync(Guid userId, string code, CancellationToken cancellationToken = default);
}
