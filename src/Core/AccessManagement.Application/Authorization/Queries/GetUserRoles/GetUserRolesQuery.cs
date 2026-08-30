using AccessManagement.Application.Authorization.Services;
using AccessManagement.Application.Common.Exceptions;
using AccessManagement.Application.Common.Interfaces;
using AccessManagement.Domain.Authorization.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AccessManagement.Application.Authorization.Queries.GetUserRoles;

public record GetUserRolesQuery(Guid UserId) : IRequest<IReadOnlyList<UserAssignedRoleDto>>;

public record UserAssignedRoleDto(
    Guid RoleId,
    string RoleCode,
    string RoleName,
    DateTimeOffset ValidFrom,
    DateTimeOffset? ValidTo,
    int GrantsCount,
    bool IsActive);

public class GetUserRolesQueryHandler : IRequestHandler<GetUserRolesQuery, IReadOnlyList<UserAssignedRoleDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICompanyVisibilityService _visibility;
    private readonly ICurrentUser _currentUser;

    public GetUserRolesQueryHandler(
        IApplicationDbContext context,
        ICompanyVisibilityService visibility,
        ICurrentUser currentUser)
    {
        _context = context;
        _visibility = visibility;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyList<UserAssignedRoleDto>> Handle(GetUserRolesQuery request, CancellationToken cancellationToken)
    {
        var vis = await _visibility.ResolveAsync(cancellationToken);
        if (!vis.IsAdmin && _currentUser.UserId != request.UserId && !vis.UserIds.Contains(request.UserId))
        {
            throw new ForbiddenAccessException();
        }

        var now = DateTimeOffset.UtcNow;

        var userRoleGrants = await _context.Grants
            .AsNoTracking()
            .Where(g => g.SubjectType == SubjectType.User
                     && g.SubjectUserId == request.UserId
                     && g.SourceType == SourceType.Role
                     && (g.ValidTo == null || g.ValidTo > now))
            .ToListAsync(cancellationToken);

        var roleIds = userRoleGrants.Select(g => g.SourceId).Distinct().ToList();
        var roles = await _context.AuthorizationRoles
            .AsNoTracking()
            .Where(r => roleIds.Contains(r.Id))
            .ToDictionaryAsync(r => r.Id, cancellationToken);

        return userRoleGrants
            .GroupBy(g => g.SourceId)
            .Select(grp =>
            {
                var roleId = grp.Key;
                roles.TryGetValue(roleId, out var role);
                var minValidFrom = grp.Min(g => g.ValidFrom);
                var maxValidTo = grp.Any(g => g.ValidTo == null) ? null : grp.Max(g => g.ValidTo);

                return new UserAssignedRoleDto(
                    roleId,
                    role?.Code ?? $"ROLE-{roleId}",
                    role?.Name ?? "Unknown Role",
                    minValidFrom,
                    maxValidTo,
                    grp.Count(),
                    minValidFrom <= now && (maxValidTo == null || maxValidTo >= now));
            })
            .OrderBy(r => r.RoleCode)
            .ToList();
    }
}
