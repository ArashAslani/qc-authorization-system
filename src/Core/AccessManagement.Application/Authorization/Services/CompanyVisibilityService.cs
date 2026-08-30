using AccessManagement.Application.Abstractions;
using AccessManagement.Application.Common.Exceptions;
using AccessManagement.Application.Common.Interfaces;
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

    public CompanyVisibilityService(
        ICurrentUser currentUser,
        IActorAccessService actorAccess,
        IApplicationDbContext db,
        IOrganizationalUnitHierarchy units)
    {
        _currentUser = currentUser;
        _actorAccess = actorAccess;
        _db = db;
        _units = units;
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
            return;
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
            return new CompanyVisibility(
                true,
                _currentUser.ActiveCompanyId,
                new HashSet<Guid>(),
                new HashSet<Guid>(),
                new HashSet<Guid>(),
                new HashSet<Guid>());
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

        var positionIds = await _db.Positions
            .AsNoTracking()
            .Where(p => p.CompanyUnitId == companyId)
            .Select(p => p.Id)
            .ToListAsync(ct);

        var positionSet = positionIds.ToHashSet();
        var now = DateTimeOffset.UtcNow;

        var personnelRows = await _db.PositionAssignments
            .AsNoTracking()
            .Where(a => positionSet.Contains(a.PositionId)
                     && a.ValidFrom <= now
                     && (a.ValidTo == null || now <= a.ValidTo))
            .Select(a => new { a.PersonnelId, a.Personnel.IdentityUserId })
            .ToListAsync(ct);

        var personnelIds = personnelRows.Select(r => r.PersonnelId).ToHashSet();
        var userIds = personnelRows
            .Where(r => r.IdentityUserId.HasValue)
            .Select(r => r.IdentityUserId!.Value)
            .ToHashSet();

        if (_currentUser.UserId is Guid self)
        {
            userIds.Add(self);
        }

        return new CompanyVisibility(false, companyId, unitIds, positionSet, personnelIds, userIds);
    }
}
