using qc_authorization.Domain.Authorization;
using qc_authorization.Domain.Authorization.Enums;

namespace qc_authorization.Application.Authorization.Evaluation;

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
    /// Case-insensitive form for matching against the catalog. Permission
    /// codes in this system are conventionally upper-case (e.g.
    /// <c>PERSONNEL.READ</c>) but comparisons must not be case-sensitive.
    /// </summary>
    public string NormalizedPermissionCode => PermissionCode.ToUpperInvariant();
}
