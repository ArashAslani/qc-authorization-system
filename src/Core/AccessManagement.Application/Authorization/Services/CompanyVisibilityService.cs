using AccessManagement.Application.Abstractions;
using AccessManagement.Application.Common.Exceptions;
using AccessManagement.Application.Common.Interfaces;
using AccessManagement.Domain.Organization.Enums;
using Microsoft.EntityFrameworkCore;

namespace AccessManagement.Application.Authorization.Services;

public interface ICompanyVisibilityService
{
    Task<bool> IsAdminAsync(CancellationToken ct = default);

    Task EnsureAuditReaderAsync(CancellationToken ct = default);

    Task<CompanyVisibility> ResolveAsync(CancellationToken ct = default);
}

public sealed record CompanyVisibility(
    bool IsAdmin,
    Guid? CompanyUnitId,
    IReadOnlySet<Guid> UnitIds,
    IReadOnlySet<Guid> PositionIds,
    IReadOnlySet<Guid> PersonnelIds,
    IReadOnlySet<Guid> UserIds);

public sealed class CompanyVisibilityService : ICompanyVisibilityService
{
    private readonly ICurrentUser _currentUser;
    private readonly IActorAccessService _actorAccess;
    private readonly IApplicationDbContext _db;
    private readonly IOrganizationalUnitHierarchy _units;
    private readonly LineManagerTargetPolicy _targets;

    public CompanyVisibilityService(
        ICurrentUser currentUser,
        IActorAccessService actorAccess,
        IApplicationDbContext db,
        IOrganizationalUnitHierarchy units,
        LineManagerTargetPolicy targets)
    {
        _currentUser = currentUser;
        _actorAccess = actorAccess;
        _db = db;
        _units = units;
        _targets = targets;
    }

    public async Task<bool> IsAdminAsync(CancellationToken ct = default)
    {
        if (_currentUser.UserId is not Guid userId)
        {
            return false;
        }

        return await _actorAccess.IsUserAdminAsync(userId, _currentUser.ActiveCompanyId, ct);
    }

    public async Task EnsureAuditReaderAsync(CancellationToken ct = default)
    {
        if (_currentUser.UserId is null)
        {
            throw new UnauthorizedAccessException();
        }

        if (!await IsAdminAsync(ct))
        {
            throw new ForbiddenAccessException();
        }
    }

    public async Task<CompanyVisibility> ResolveAsync(CancellationToken ct = default)
    {
        if (_currentUser.UserId is not Guid userId)
        {
            throw new UnauthorizedAccessException();
        }

        var isAdmin = await _actorAccess.IsUserAdminAsync(userId, _currentUser.ActiveCompanyId, ct);
        if (isAdmin)
        {
            return new CompanyVisibility(
                true,
                _currentUser.ActiveCompanyId,
                new HashSet<Guid>(),
                new HashSet<Guid>(),
                new HashSet<Guid>(),
                new HashSet<Guid>());
        }

        var companyId = _currentUser.ActiveCompanyId
            ?? throw new ForbiddenAccessException();

        var descendantIds = await _units.GetDescendantIdsAsync(companyId, ct);
        var unitIds = descendantIds.Append(companyId).ToHashSet();

        var actorPositionIds = await _targets.GetActorPositionIdsAsync(userId, companyId, ct);
        var subordinatePositionIds = await _targets.GetSubordinatePositionIdsAsync(actorPositionIds, ct);
        var visiblePositions = actorPositionIds.Concat(subordinatePositionIds).ToHashSet();

        var now = DateTimeOffset.UtcNow;
        var personnelRows = visiblePositions.Count == 0
            ? []
            : await _db.PositionAssignments
                .AsNoTracking()
                .Where(a => visiblePositions.Contains(a.PositionId)
                         && a.ValidFrom <= now
                         && (a.ValidTo == null || now <= a.ValidTo)
                         && a.Personnel.Status == PersonnelStatus.Active)
                .Select(a => new { a.PersonnelId, a.Personnel.IdentityUserId })
                .ToListAsync(ct);

        var personnelIds = personnelRows.Select(r => r.PersonnelId).ToHashSet();
        var userIds = personnelRows
            .Where(r => r.IdentityUserId.HasValue)
            .Select(r => r.IdentityUserId!.Value)
            .ToHashSet();

        userIds.Add(userId);
        var selfPersonnelId = await _db.Personnel
            .AsNoTracking()
            .Where(p => p.IdentityUserId == userId)
            .Select(p => (Guid?)p.Id)
            .FirstOrDefaultAsync(ct);
        if (selfPersonnelId is Guid pid)
        {
            personnelIds.Add(pid);
        }

        return new CompanyVisibility(false, companyId, unitIds, visiblePositions, personnelIds, userIds);
    }
}
