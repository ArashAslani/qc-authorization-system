namespace AccessManagement.Domain.Authorization;

/// <summary>
/// Core-owned permission codes seeded by Access Management, not by a product plugin.
/// </summary>
public static class CoreAccessPermissions
{
    public const string PluginCode = "CORE";

    public const string Grant = "ACCESS.GRANT";
    public const string Revoke = "ACCESS.REVOKE";
    public const string AdministerAll = "ACCESS.ADMINISTER_ALL";
}
