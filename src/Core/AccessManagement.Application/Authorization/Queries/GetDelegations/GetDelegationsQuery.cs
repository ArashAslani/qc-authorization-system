using AccessManagement.Application.Authorization.Services;
using AccessManagement.Application.Common.Interfaces;
using AccessManagement.Application.Common.Models;
using AccessManagement.Domain.Authorization.ValueObjects;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AccessManagement.Application.Authorization.Queries.GetDelegations;

public record GetDelegationsQuery(
    Guid? DelegatorUserId = null,
    Guid? DelegateUserId = null,
    Guid? PermissionId = null,
    bool? ActiveOnly = null,
    int PageNumber = 1,
    int PageSize = PaginatedList<DelegationDto>.DefaultPageSize) : IRequest<PaginatedList<DelegationDto>>;

public record DelegationDto(
    Guid Id,
    Guid DelegatorUserId,
    Guid DelegateUserId,
    Guid PermissionId,
    string PermissionCode,
    DateTimeOffset ValidFrom,
    DateTimeOffset? ValidTo,
    Guid? ScopeUnitId,
    bool Delegable,
    bool IsRevoked,
    bool IsActive,
    Guid? ParentDelegationId);

public class GetDelegationsQueryHandler : IRequestHandler<GetDelegationsQuery, PaginatedList<DelegationDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICompanyVisibilityService _visibility;
    private readonly ICurrentUser _currentUser;

    public GetDelegationsQueryHandler(
        IApplicationDbContext context,
        ICompanyVisibilityService visibility,
        ICurrentUser currentUser)
    {
        _context = context;
        _visibility = visibility;
        _currentUser = currentUser;
    }

    public async Task<PaginatedList<DelegationDto>> Handle(GetDelegationsQuery request, CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var query = _context.Delegations
            .AsNoTracking()
            .Include(d => d.Permission)
            .AsQueryable();

        var vis = await _visibility.ResolveAsync(cancellationToken);
        if (!vis.IsAdmin)
        {
            var self = _currentUser.UserId;
            var userIds = vis.UserIds.ToList();
            query = query.Where(d =>
                (self != null && (d.DelegatorUserId == self || d.DelegateUserId == self))
                || userIds.Contains(d.DelegatorUserId)
                || userIds.Contains(d.DelegateUserId));
        }

        if (request.DelegatorUserId.HasValue)
        {
            query = query.Where(d => d.DelegatorUserId == request.DelegatorUserId.Value);
        }

        if (request.DelegateUserId.HasValue)
        {
            query = query.Where(d => d.DelegateUserId == request.DelegateUserId.Value);
        }

        if (request.PermissionId.HasValue)
        {
            query = query.Where(d => d.PermissionId == request.PermissionId.Value);
        }

        if (request.ActiveOnly == true)
        {
            query = query.Where(d => !d.IsRevoked && d.ValidFrom <= now && (d.ValidTo == null || d.ValidTo >= now));
        }

        var (pageNumber, pageSize) = PaginatedList<DelegationDto>.Normalize(request.PageNumber, request.PageSize);
        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(d => d.Id)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(d => new DelegationDto(
                d.Id,
                d.DelegatorUserId,
                d.DelegateUserId,
                d.PermissionId,
                d.Permission.Code,
                d.ValidFrom,
                d.ValidTo,
                d.ScopeUnitId,
                d.Delegable,
                d.IsRevoked,
                !d.IsRevoked && d.ValidFrom <= now && (d.ValidTo == null || d.ValidTo >= now),
                d.ParentDelegationId))
            .ToListAsync(cancellationToken);

        return new PaginatedList<DelegationDto>(items, totalCount, pageNumber, pageSize);
    }
}
