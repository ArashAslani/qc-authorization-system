namespace AccessManagement.Application.Common.Interfaces;

public interface ITokenRevocationStore
{
    Task RevokeAsync(string jti, Guid userId, DateTimeOffset expiresAtUtc, CancellationToken cancellationToken = default);

    Task<bool> IsRevokedAsync(string jti, CancellationToken cancellationToken = default);
}
