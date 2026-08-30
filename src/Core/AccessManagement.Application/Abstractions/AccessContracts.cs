using AccessManagement.Application.Common.Interfaces;
using AccessManagement.Domain.Authorization;
using AccessManagement.Domain.Authorization.Evaluation;

namespace AccessManagement.Application.Abstractions;

public interface IAccessEvaluator
{
    Task<AccessDecision> EvaluateAsync(AccessRequest request, CancellationToken ct = default);

    Task<AccessibleScopeResult> GetAccessibleScopesAsync(
        Guid subjectUserId,
        Guid? activePositionId,
        string permissionCode,
        CancellationToken ct = default);
}

public sealed record AccessibleScopeResult(
    bool IsUnrestricted,
    IReadOnlyList<Guid> ScopeRootUnitIds);

public interface IGrantResolver
{
    Task<IReadOnlyList<Grant>> FindCandidatesAsync(AccessRequest request, CancellationToken ct = default);
}

public interface IScopeMatcher
{
    Task<bool> MatchesAsync(Guid? grantScopeUnitId, Guid? resourceScopeUnitId, CancellationToken ct = default);
}

public interface IOrganizationalUnitHierarchy
{
    Task<bool> IsDescendantOfAsync(Guid unitId, Guid ancestorId, CancellationToken ct = default);

    Task<IReadOnlyList<Guid>> GetDescendantIdsAsync(Guid unitId, CancellationToken ct = default);

    Task<string?> GetUnitTypeAsync(Guid unitId, CancellationToken ct = default);
}

public interface IPositionHierarchyQuery
{
    Task<IReadOnlyList<Guid>> GetAncestorsAsync(Guid positionId, CancellationToken ct = default);

    Task<IReadOnlyList<Guid>> GetDescendantsAsync(Guid positionId, CancellationToken ct = default);
}

public interface IDecisionTraceWriter
{
    Task<AccessDecision> WriteAsync(
        AccessRequest request,
        IReadOnlyList<Grant> candidates,
        Grant? winner,
        AccessDecision decision,
        CancellationToken ct = default);
}

public interface IAccessPluginSeeder
{
    string PluginCode { get; }

    Task SeedAsync(IApplicationDbContext db, CancellationToken ct = default);
}
