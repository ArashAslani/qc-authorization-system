using AccessManagement.Domain.Authorization.Enums;

namespace AccessManagement.Domain.Authorization.Evaluation;

public sealed record AccessRequest(
    Guid SubjectUserId,
    Guid? ActivePositionId,
    string PermissionCode,
    Guid? ResourceScopeUnitId,
    DateTimeOffset When,
    IReadOnlyDictionary<string, object>? Context = null)
{
    public string NormalizedPermissionCode => PermissionCode.Trim().ToUpperInvariant();

    public string Resource =>
        PermissionCode.Contains('.', StringComparison.Ordinal)
            ? PermissionCode[..PermissionCode.LastIndexOf('.')]
            : PermissionCode;

    public string Action =>
        PermissionCode.Contains('.', StringComparison.Ordinal)
            ? PermissionCode[(PermissionCode.LastIndexOf('.') + 1)..]
            : PermissionCode;

    public static AccessRequest ForUser(
        Guid userId,
        string permissionCode,
        Guid? activePositionId = null,
        Guid? resourceScopeUnitId = null,
        DateTimeOffset? when = null) =>
        new(userId, activePositionId, permissionCode, resourceScopeUnitId, when ?? DateTimeOffset.UtcNow);

    public static AccessRequest ForUser(
        Guid userId,
        string action,
        string resource,
        string? resourceId,
        DateTimeOffset when,
        Guid? activePositionId = null,
        Guid? resourceScopeUnitId = null) =>
        new(userId, activePositionId, $"{resource}.{action}", resourceScopeUnitId ?? ParseGuid(resourceId), when);

    private static Guid? ParseGuid(string? value) =>
        Guid.TryParse(value, out var id) ? id : null;
}
