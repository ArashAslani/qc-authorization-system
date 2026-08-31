using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace AccessManagement.Infrastructure.Identity;

public sealed class JwtTokenService
{
    public const string ActiveCompanyIdClaim = "active_company_id";
    public const string SecurityStampClaim = "security_stamp";
    public const string TokenUseClaim = "token_use";
    public const string TokenUseAccess = "access";
    public const string TokenUseTwoFactor = "2fa";

    private readonly JwtOptions _options;

    public JwtTokenService(IOptions<JwtOptions> options) => _options = options.Value;

    public string GenerateToken(ApplicationUser user) =>
        GenerateToken(user, activeCompanyId: null);

    public string GenerateToken(ApplicationUser user, Guid? activeCompanyId, int? expiryMinutes = null) =>
        Write(user, activeCompanyId, TokenUseAccess, expiryMinutes ?? _options.ExpiryMinutes);

    public string GenerateTwoFactorToken(ApplicationUser user) =>
        Write(user, activeCompanyId: null, TokenUseTwoFactor, expiryMinutes: 10);

    public bool TryReadToken(string token, out JwtSecurityToken jwt)
    {
        jwt = null!;
        try
        {
            var handler = new JwtSecurityTokenHandler();
            handler.ValidateToken(token, CreateValidationParameters(), out var validated);
            jwt = (JwtSecurityToken)validated;
            return true;
        }
        catch
        {
            return false;
        }
    }

    public TokenValidationParameters CreateValidationParameters() =>
        new()
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = _options.Issuer,
            ValidAudience = _options.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.Key)),
            ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
            ClockSkew = TimeSpan.FromMinutes(1),
        };

    private string Write(ApplicationUser user, Guid? activeCompanyId, string tokenUse, int expiryMinutes)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(TokenUseClaim, tokenUse),
        };

        if (!string.IsNullOrWhiteSpace(user.Email))
        {
            claims.Add(new Claim(ClaimTypes.Email, user.Email));
        }

        if (!string.IsNullOrWhiteSpace(user.UserName))
        {
            claims.Add(new Claim(ClaimTypes.Name, user.UserName));
        }

        if (user.PersonnelId.HasValue)
        {
            claims.Add(new Claim("personnel_id", user.PersonnelId.Value.ToString()));
        }

        if (activeCompanyId.HasValue)
        {
            claims.Add(new Claim(ActiveCompanyIdClaim, activeCompanyId.Value.ToString()));
        }

        if (!string.IsNullOrWhiteSpace(user.SecurityStamp))
        {
            claims.Add(new Claim(SecurityStampClaim, user.SecurityStamp));
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.Key));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var jwt = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expiryMinutes),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(jwt);
    }
}
