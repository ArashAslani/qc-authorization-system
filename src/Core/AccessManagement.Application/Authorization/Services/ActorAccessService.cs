using AccessManagement.Application.Abstractions;
using AccessManagement.Application.Common.Interfaces;
using AccessManagement.Application.Session;
using AccessManagement.Domain.Authorization;
using AccessManagement.Domain.Authorization.Evaluation;
using Microsoft.EntityFrameworkCore;

namespace AccessManagement.Application.Authorization.Services;

public interface IActorAccessService
{
    Task<bool> HasPermissionAsync(
        Guid userId,
        Guid? companyUnitId,
        string permissionCode,
        Guid? scopeUnitId,
        CancellationToken ct = default);

    Task<AccessDecision> EvaluateAsync(
        Guid userId,
        Guid? companyUnitId,
        string permissionCode,
        Guid? scopeUnitId,
        CancellationToken ct = default);

    Task<bool> IsUserAdminAsync(Guid userId, Guid? companyUnitId, CancellationToken ct = default);

    Task<AccessibleScopeResult> GetAccessibleRootsAsync(
        Guid userId,
        Guid companyUnitId,
        string permissionCode,
        CancellationToken ct = default);
}

public sealed class ActorAccessService : IActorAccessService
{
    private readonly CompanyWorkspaceService _workspace;
    private readonly IAccessEvaluator _evaluator;
    private readonly IApplicationDbContext _db;

    public ActorAccessService(
        CompanyWorkspaceService workspace,
        IAccessEvaluator evaluator,
        IApplicationDbContext db)
    {
        _workspace = workspace;
        _evaluator = evaluator;
        _db = db;
    }

    public async Task<bool> HasPermissionAsync(
        Guid userId,
        Guid? companyUnitId,
        string permissionCode,
        Guid? scopeUnitId,
        CancellationToken ct = default)
    {
        var decision = await EvaluateAsync(userId, companyUnitId, permissionCode, scopeUnitId, ct);
        return decision.Allowed;
    }

    public Task<AccessDecision> EvaluateAsync(
        Guid userId,
        Guid? companyUnitId,
        string permissionCode,
        Guid? scopeUnitId,
        CancellationToken ct = default)
    {
        if (companyUnitId is Guid company)
        {
            return _workspace.EvaluateInCompanyAsync(userId, company, permissionCode, scopeUnitId, ct);
        }

        return Task.FromResult(AccessDecision.Deny(Guid.NewGuid(), AccessDecisionReasons.NoGrant));
    }

    public async Task<bool> IsUserAdminAsync(Guid userId, Guid? companyUnitId, CancellationToken ct = default)
    {
        var isSystem = await _db.Personnel
            .AsNoTracking()
            .AnyAsync(p => p.IdentityUserId == userId && p.IsSystemUser, ct);
        if (isSystem)
        {
            return true;
        }

        if (companyUnitId is null)
        {
            return false;
        }

        return await HasPermissionAsync(userId, companyUnitId, CoreAccessPermissions.AdministerAll, companyUnitId, ct);
    }

    public async Task<AccessibleScopeResult> GetAccessibleRootsAsync(
        Guid userId,
        Guid companyUnitId,
        string permissionCode,
        CancellationToken ct = default)
    {
        var positions = await _workspace.GetActivePositionsAsync(userId, companyUnitId, ct);
        if (positions.Count == 0)
        {
            return await _evaluator.GetAccessibleScopesAsync(userId, null, permissionCode, ct);
        }

        var unrestricted = false;
        var roots = new HashSet<Guid>();
        var denied = new HashSet<Guid>();
        foreach (var positionId in positions)
        {
            var slice = await _evaluator.GetAccessibleScopesAsync(userId, positionId, permissionCode, ct);
            if (slice.IsUnrestricted)
            {
                unrestricted = true;
            }

            foreach (var root in slice.ScopeRootUnitIds)
            {
                roots.Add(root);
            }

            foreach (var hole in slice.DeniedScopeUnitIds)
            {
                denied.Add(hole);
            }
        }

        if (unrestricted)
        {
            return new AccessibleScopeResult(true, Array.Empty<Guid>(), denied.ToList());
        }

        return new AccessibleScopeResult(false, roots.ToList(), denied.ToList());
    }
}
