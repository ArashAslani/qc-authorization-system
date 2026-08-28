using qc_authorization.Domain.Authorization.Enums;

namespace qc_authorization.Domain.Authorization.Evaluation;

/// <summary>
/// Input to the Access Evaluation Engine. The Engine is the only
/// component that may return an <see cref="AccessDecision"/>.
/// </summary>
public sealed record AccessRequest(
    SubjectType SubjectType,
    int SubjectId,
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
}
