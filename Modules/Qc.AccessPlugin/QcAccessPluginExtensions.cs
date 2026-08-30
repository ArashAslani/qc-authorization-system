using AccessManagement.Application.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Qc.AccessPlugin.ControlPlans;

namespace Qc.AccessPlugin;

public static class QcAccessPluginExtensions
{
    /// <summary>
    /// Registers the QC access plugin seeder and sample Control Plan guard.
    /// Call from the QC host after <c>AddAccessManagementCore()</c>.
    /// </summary>
    public static IServiceCollection AddQcAccessPlugin(this IServiceCollection services)
    {
        services.AddAccessPlugin<QcAccessSeeder>();
        services.AddScoped<ControlPlanApprovalGuard>();
        services.AddSingleton<IControlPlanStore, InMemoryControlPlanStore>();
        return services;
    }
}
