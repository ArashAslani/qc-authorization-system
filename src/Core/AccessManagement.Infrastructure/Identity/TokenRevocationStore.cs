using AccessManagement.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;
using AccessManagement.Infrastructure.Data;

namespace AccessManagement.Infrastructure.Identity;

public sealed class TokenRevocationStore : ITokenRevocationStore
{
    private readonly ApplicationDbContext _db;

    public TokenRevocationStore(ApplicationDbContext db) => _db = db;

    public async Task RevokeAsync(string jti, Guid userId, DateTimeOffset expiresAtUtc, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(jti))
        {
            return;
        }

        var exists = await _db.RevokedAccessTokens.AnyAsync(t => t.Jti == jti, cancellationToken);
        if (exists)
        {
            return;
        }

        _db.RevokedAccessTokens.Add(new RevokedAccessToken
        {
            Jti = jti,
            UserId = userId,
            ExpiresAtUtc = expiresAtUtc,
        });
        await _db.SaveChangesAsync(cancellationToken);
    }

    public Task<bool> IsRevokedAsync(string jti, CancellationToken cancellationToken = default) =>
        _db.RevokedAccessTokens.AsNoTracking().AnyAsync(t => t.Jti == jti, cancellationToken);
}
