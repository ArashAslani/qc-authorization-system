namespace qc_authorization.Application.Common.Interfaces;

/// <summary>
/// Typed, deterministic authorization context supplied by a business module
/// to the application layer for scope evaluation.
/// </summary>
public sealed record ResourceAuthorizationContext(
    string Resource,
    string? ResourceId = null,
    Guid? HoldingId = null,
    Guid? CompanyId = null,
    Guid? LaboratoryId = null,
    Guid? WorkstationId = null,
    string? CustomScope = null)
{
    public IReadOnlyDictionary<string, object> ToContextDictionary()
    {
        var dict = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        if (HoldingId.HasValue) dict["HoldingId"] = HoldingId.Value;
        if (CompanyId.HasValue) dict["CompanyId"] = CompanyId.Value;
        if (LaboratoryId.HasValue) dict["LaboratoryId"] = LaboratoryId.Value;
        if (WorkstationId.HasValue) dict["WorkstationId"] = WorkstationId.Value;
        if (!string.IsNullOrWhiteSpace(CustomScope)) dict["CustomScope"] = CustomScope;
        return dict;
    }
}

/// <summary>
/// Small, explicit provider contract implemented by business modules to resolve
/// organizational and data scope dimensions for a specific resource key.
/// </summary>
public interface IResourceAuthorizationContextProvider<in TKey>
{
    Task<ResourceAuthorizationContext> GetContextAsync(TKey resourceKey, CancellationToken cancellationToken = default);
}
