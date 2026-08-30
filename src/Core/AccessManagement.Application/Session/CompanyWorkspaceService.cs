using AccessManagement.Application.Abstractions;
using AccessManagement.Application.Common.Interfaces;
using AccessManagement.Domain.Authorization.Evaluation;
using Microsoft.EntityFrameworkCore;

namespace AccessManagement.Application.Session;

/// <summary>
/// Session / company-workspace wrapper. The only place that unions
/// multiple active positions inside one company. <see cref="IAccessEvaluator"/>
/// is unaware of company switching.
/// </summary>
public sealed class CompanyWorkspaceService
{
    private readonly IAccessEvaluator _evaluator;
    private readonly IApplicationDbContext _db;

    public CompanyWorkspaceService(IAccessEvaluator evaluator, IApplicationDbContext db)
    {
        _evaluator = evaluator;
        _db = db;
    }

    public async Task<IReadOnlyList<Guid>> GetActivePositionsAsync(Guid userId, Guid companyUnitId, CancellationToken ct = default)
    {
        var now = DateTimeOffset.UtcNow;
        return await _db.PositionAssignments
            .AsNoTracking()
            .Include(a => a.Position)
            .Where(a => a.Personnel.IdentityUserId == userId
                     && a.Position.CompanyUnitId == companyUnitId
                     && a.ValidFrom <= now
                     && (a.ValidTo == null || now <= a.ValidTo))
            .Select(a => a.PositionId)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<Guid>> GetAllActivePositionsAsync(Guid userId, CancellationToken ct = default)
    {
        var now = DateTimeOffset.UtcNow;
        return await _db.PositionAssignments
            .AsNoTracking()
            .Include(a => a.Position)
            .Where(a => a.Personnel.IdentityUserId == userId
                     && a.ValidFrom <= now
                     && (a.ValidTo == null || now <= a.ValidTo))
            .Select(a => a.PositionId)
            .ToListAsync(ct);
    }

    public async Task<AccessDecision> EvaluateInCompanyAsync(
        Guid userId,
        Guid companyUnitId,
        string permissionCode,
        Guid? scopeUnitId,
        CancellationToken ct = default)
    {
        var positions = await GetActivePositionsAsync(userId, companyUnitId, ct);
        return await EvaluateForPositionsAsync(userId, positions, permissionCode, scopeUnitId, ct);
    }

    public async Task<AccessDecision> EvaluateAcrossCompaniesAsync(
        Guid userId,
        string permissionCode,
        Guid? scopeUnitId,
        CancellationToken ct = default)
    {
        var positions = await GetAllActivePositionsAsync(userId, ct);
        return await EvaluateForPositionsAsync(userId, positions, permissionCode, scopeUnitId, ct);
    }

    public async Task<bool> HasPermissionAsync(
        Guid userId,
        Guid companyUnitId,
        string permissionCode,
        Guid? scopeUnitId,
        CancellationToken ct = default)
    {
        var decision = await EvaluateInCompanyAsync(userId, companyUnitId, permissionCode, scopeUnitId, ct);
        return decision.Allowed;
    }

    private async Task<AccessDecision> EvaluateForPositionsAsync(
        Guid userId,
        IReadOnlyList<Guid> positions,
        string permissionCode,
        Guid? scopeUnitId,
        CancellationToken ct)
    {
        var when = DateTimeOffset.UtcNow;
        if (positions.Count == 0)
        {
            return await _evaluator.EvaluateAsync(
                new AccessRequest(userId, null, permissionCode, scopeUnitId, when), ct);
        }

        AccessDecision? lastDeny = null;
        foreach (var positionId in positions)
        {
            var decision = await _evaluator.EvaluateAsync(
                new AccessRequest(userId, positionId, permissionCode, scopeUnitId, when), ct);
            if (decision.Allowed)
            {
                return decision;
            }

            lastDeny = decision;
        }

        return lastDeny!;
    }
}
