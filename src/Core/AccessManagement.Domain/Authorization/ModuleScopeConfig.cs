using AccessManagement.Domain.Authorization.Exceptions;
using AccessManagement.Domain.Common;

namespace AccessManagement.Domain.Authorization;

/// <summary>
/// Core table filled by plugins. Caps how deep a resource's grant scope may go.
/// </summary>
public class ModuleScopeConfig : BaseAuditableEntity, IAggregateRoot
{
    private ModuleScopeConfig() { }

    public string ResourceCode { get; private set; } = string.Empty;
    public string MaxScopeUnitType { get; private set; } = string.Empty;

    public static ModuleScopeConfig Create(string resourceCode, string maxScopeUnitType)
    {
        if (string.IsNullOrWhiteSpace(resourceCode))
        {
            throw new AuthorizationDomainException("ResourceCode is required.");
        }

        if (string.IsNullOrWhiteSpace(maxScopeUnitType))
        {
            throw new AuthorizationDomainException("MaxScopeUnitType is required.");
        }

        return new ModuleScopeConfig
        {
            ResourceCode = resourceCode.Trim().ToUpperInvariant(),
            MaxScopeUnitType = maxScopeUnitType.Trim(),
        };
    }
}
