namespace AccessManagement.Application.Common.Interfaces;

/// <summary>
/// Product modules map a business record to a Core scope unit.
/// </summary>
public sealed record ResourceAuthorizationContext(
    string Resource,
    Guid? ScopeUnitId = null,
    string? ResourceId = null);

/// <summary>
/// Implemented by product plugins to resolve the OrganizationalUnit for a resource.
/// </summary>
public interface IResourceAuthorizationContextProvider<in TKey>
{
    Task<ResourceAuthorizationContext> GetContextAsync(TKey resourceKey, CancellationToken cancellationToken = default);
}
