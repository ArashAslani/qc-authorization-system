using AccessManagement.Application.Abstractions;
using AccessManagement.Application.Common.Interfaces;
using AccessManagement.Domain.Authorization;
using AccessManagement.Domain.Organization;

namespace Qc.AccessPlugin;

/// <summary>
/// Seeds QC permissions and ModuleScopeConfig rows. Copy this project into
/// the QC product and register with <c>services.AddAccessPlugin&lt;QcAccessSeeder&gt;()</c>.
/// Organizational data and Grants are configured by the QC host, not by Core.
/// </summary>
public sealed class QcAccessSeeder : IAccessPluginSeeder
{
    public string PluginCode => QcPermissions.PluginCode;

    public async Task SeedAsync(IApplicationDbContext db, CancellationToken ct = default)
    {
        await EnsurePermission(db, QcPermissions.LaboratoryRead, "LABORATORY", "READ");
        await EnsurePermission(db, QcPermissions.LaboratoryWrite, "LABORATORY", "WRITE");
        await EnsurePermission(db, QcPermissions.ControlPlanRead, "CONTROLPLAN", "READ");
        await EnsurePermission(db, QcPermissions.ControlPlanUpdate, "CONTROLPLAN", "UPDATE");
        await EnsurePermission(db, QcPermissions.ControlPlanApprove, "CONTROLPLAN", "APPROVE");
        await EnsurePermission(db, QcPermissions.BomUpdate, "BOM", "UPDATE");

        await EnsureScope(db, "LABORATORY", QcPermissions.SuggestedUnitTypes.Workstation);
        await EnsureScope(db, "CONTROLPLAN", OrganizationalUnitTypes.Company);
        await EnsureScope(db, "BOM", OrganizationalUnitTypes.Company);

        await db.SaveChangesAsync(ct);
    }

    private static Task EnsurePermission(
        IApplicationDbContext db,
        string code,
        string resource,
        string action)
    {
        if (!db.Permissions.Any(p => p.Code == code))
        {
            db.Permissions.Add(Permission.Create(code, resource, action, pluginCode: QcPermissions.PluginCode));
        }

        return Task.CompletedTask;
    }

    private static Task EnsureScope(
        IApplicationDbContext db,
        string resourceCode,
        string maxScopeUnitType)
    {
        if (!db.ModuleScopeConfigs.Any(c => c.ResourceCode == resourceCode))
        {
            db.ModuleScopeConfigs.Add(ModuleScopeConfig.Create(resourceCode, maxScopeUnitType));
        }

        return Task.CompletedTask;
    }
}
