namespace qc_authorization.Infrastructure.Identity;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Key { get; set; } = string.Empty;

    public string Issuer { get; set; } = "qc-authorization";

    public string Audience { get; set; } = "qc-authorization";

    public int ExpiryMinutes { get; set; } = 60;
}
