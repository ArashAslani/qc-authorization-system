using AccessManagement.Application.Authorization.Services;
using AccessManagement.Application.Common.Interfaces;
using AccessManagement.Domain.Authorization.Enums;
using AccessManagement.Domain.Authorization.ValueObjects;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AccessManagement.Application.Authorization.Queries.GetGrants;

public record GetGrantsQuery(
    SubjectType? SubjectType = null,
    Guid? SubjectId = null,
    Guid? SubjectUserId = null,
    Guid? PermissionId = null,
    Effect? Effect = null,
    SourceType? SourceType = null,
    bool? ActiveOnly = null) : IRequest<IReadOnlyList<GrantDto>>;

public record GrantDto(
    Guid Id,
    SubjectType SubjectType,
    Guid SubjectId,
    Guid? SubjectUserId,
    Guid PermissionId,
    string PermissionCode,
    string? Resource,
    string? ResourceId,
    Guid? ScopeUnitId,
    Effect Effect,
    SourceType SourceType,
    Guid SourceId,
    Guid? SourceUserId,
    DateTimeOffset ValidFrom,
    DateTimeOffset? ValidTo,
    int Priority,
    bool IsActive);

public class GetGrantsQueryHandler : IRequestHandler<GetGrantsQuery, IReadOnlyList<GrantDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICompanyVisibilityService _visibility;

    public GetGrantsQueryHandler(IApplicationDbContext context, ICompanyVisibilityService visibility)
    {
        _context = context;
        _visibility = visibility;
    }

    public async Task<IReadOnlyList<GrantDto>> Handle(GetGrantsQuery request, CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var query = _context.Grants
            .AsNoTracking()
            .Include(g => g.Permission)
            .AsQueryable();

        var vis = await _visibility.ResolveAsync(cancellationToken);
        if (!vis.IsAdmin)
        {
            var positionIds = vis.PositionIds.ToList();
            var userIds = vis.UserIds.ToList();
            var unitIds = vis.UnitIds.ToList();
            query = query.Where(g =>
                (g.SubjectType == SubjectType.Position && positionIds.Contains(g.SubjectId))
                || (g.SubjectUserId != null && userIds.Contains(g.SubjectUserId.Value))
                || (g.ScopeUnitId != null && unitIds.Contains(g.ScopeUnitId.Value)));
            query = AccessibleScopeQuery.ApplyAccessibleScopes(
                query,
                unitIds,
                Array.Empty<Guid>(),
                isUnrestricted: false);
        }

        if (request.SubjectType.HasValue)
        {
            query = query.Where(g => g.SubjectType == request.SubjectType.Value);
        }

        if (request.SubjectId.HasValue)
        {
            query = query.Where(g => g.SubjectId == request.SubjectId.Value);
        }

        if (request.SubjectUserId.HasValue)
        {
            query = query.Where(g => g.SubjectUserId == request.SubjectUserId.Value);
        }

        if (request.PermissionId.HasValue)
        {
            query = query.Where(g => g.PermissionId == request.PermissionId.Value);
        }

        if (request.Effect.HasValue)
        {
            query = query.Where(g => g.Effect == request.Effect.Value);
        }

        if (request.SourceType.HasValue)
        {
            query = query.Where(g => g.SourceType == request.SourceType.Value);
        }

        if (request.ActiveOnly == true)
        {
            query = query.Where(g => g.ValidFrom <= now && (g.ValidTo == null || g.ValidTo >= now));
        }

        return await query
            .OrderByDescending(g => g.Priority)
            .ThenByDescending(g => g.Id)
            .Select(g => new GrantDto(
                g.Id,
                g.SubjectType,
                g.SubjectId,
                g.SubjectUserId,
                g.PermissionId,
                g.Permission.Code,
                g.Resource,
                g.ResourceId,
                g.ScopeUnitId,
                g.Effect,
                g.SourceType,
                g.SourceId,
                g.SourceUserId,
                g.ValidFrom,
                g.ValidTo,
                g.Priority,
                g.ValidFrom <= now && (g.ValidTo == null || g.ValidTo >= now)))
            .ToListAsync(cancellationToken);
    }
}
