using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace AccessManagement.Infrastructure.Identity;

public sealed class JwtTokenService
{
    public const string ActiveCompanyIdClaim = "active_company_id";
    public const string NationalIdClaim = "national_id";

    private readonly JwtOptions _options;

    public JwtTokenService(IOptions<JwtOptions> options) => _options = options.Value;

    public string GenerateToken(ApplicationUser user) =>
        GenerateToken(user, activeCompanyId: null, nationalId: null);

    public string GenerateToken(ApplicationUser user, Guid? activeCompanyId, string? nationalId = null)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
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

        if (!string.IsNullOrWhiteSpace(nationalId))
        {
            claims.Add(new Claim(NationalIdClaim, nationalId));
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.Key));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_options.ExpiryMinutes),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
