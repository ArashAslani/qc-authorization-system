using AccessManagement.Application.Abstractions;
using AccessManagement.Application.Common.Interfaces;
using AccessManagement.Domain.Authorization;
using AccessManagement.Domain.Authorization.Evaluation;

namespace AccessManagement.Application.Authorization;

public sealed class CoreAccessSeeder : IAccessPluginSeeder
{
    public string PluginCode => CoreAccessPermissions.PluginCode;

    public async Task SeedAsync(IApplicationDbContext db, CancellationToken ct = default)
    {
        await EnsurePermission(db, CoreAccessPermissions.Grant, "ACCESS", "GRANT", ct);
        await EnsurePermission(db, CoreAccessPermissions.Revoke, "ACCESS", "REVOKE", ct);
        await EnsurePermission(db, CoreAccessPermissions.AdministerAll, "ACCESS", "ADMINISTER_ALL", ct);
        await db.SaveChangesAsync(ct);
    }

    private static async Task EnsurePermission(
        IApplicationDbContext db,
        string code,
        string resource,
        string action,
        CancellationToken ct)
    {
        var exists = db.Permissions.Any(p => p.Code == code);
        if (exists)
        {
            return;
        }

        db.Permissions.Add(Permission.Create(code, resource, action, pluginCode: CoreAccessPermissions.PluginCode));
        await Task.CompletedTask;
    }
}
