using qc_authorization.Application.Common.Interfaces;
using qc_authorization.Application.QcBusinessIntegration.Models;

namespace qc_authorization.Application.QcBusinessIntegration.Providers;

public interface IControlPlanStore
{
    Task<ControlPlan?> FindByIdAsync(int id, CancellationToken cancellationToken = default);
    Task SaveAsync(ControlPlan controlPlan, CancellationToken cancellationToken = default);
}

public interface IBomStore
{
    Task<BOM?> FindByIdAsync(int id, CancellationToken cancellationToken = default);
    Task SaveAsync(BOM bom, CancellationToken cancellationToken = default);
}

public class ControlPlanAuthorizationContextProvider : IResourceAuthorizationContextProvider<int>
{
    private readonly IControlPlanStore _store;

    public ControlPlanAuthorizationContextProvider(IControlPlanStore store) => _store = store;

    public async Task<ResourceAuthorizationContext> GetContextAsync(int resourceKey, CancellationToken cancellationToken = default)
    {
        var plan = await _store.FindByIdAsync(resourceKey, cancellationToken);
        if (plan is null)
        {
            return new ResourceAuthorizationContext("CONTROL_PLAN", resourceKey.ToString());
        }

        return new ResourceAuthorizationContext(
            Resource: "CONTROL_PLAN",
            ResourceId: plan.Id.ToString(),
            CompanyId: plan.CompanyId,
            LaboratoryId: plan.LaboratoryId);
    }
}

public class BomAuthorizationContextProvider : IResourceAuthorizationContextProvider<int>
{
    private readonly IBomStore _store;

    public BomAuthorizationContextProvider(IBomStore store) => _store = store;

    public async Task<ResourceAuthorizationContext> GetContextAsync(int resourceKey, CancellationToken cancellationToken = default)
    {
        var bom = await _store.FindByIdAsync(resourceKey, cancellationToken);
        if (bom is null)
        {
            return new ResourceAuthorizationContext("BOM", resourceKey.ToString());
        }

        return new ResourceAuthorizationContext(
            Resource: "BOM",
            ResourceId: bom.Id.ToString(),
            CompanyId: bom.CompanyId);
    }
}
