using qc_authorization.Domain.Authorization.Enums;

namespace qc_authorization.Domain.Authorization.Evaluation;

/// <summary>
/// Input to the Access Evaluation Engine. The Engine is the only
/// component that may return an <see cref="AccessDecision"/>.
/// </summary>
public sealed record AccessRequest(
    SubjectType SubjectType,
    int SubjectId,
    Guid? UserId,
    string Action,
    string Resource,
    string? ResourceId,
    DateTimeOffset When,
    IReadOnlyDictionary<string, object>? Context = null)
{
    public string PermissionCode => $"{Resource}.{Action}";

    /// <summary>
    /// Case-insensitive form for matching against the catalog.
    /// </summary>
    public string NormalizedPermissionCode => PermissionCode.ToUpperInvariant();

    public static AccessRequest ForUser(
        Guid userId,
        string action,
        string resource,
        string? resourceId,
        DateTimeOffset when,
        IReadOnlyDictionary<string, object>? context = null) =>
        new(SubjectType.User, 0, userId, action, resource, resourceId, when, context);
}
