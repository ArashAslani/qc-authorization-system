using AccessManagement.Application.Authorization.Services;
using AccessManagement.Application.Common.Exceptions;
using AccessManagement.Application.Common.Interfaces;
using AccessManagement.Domain.Authorization.Enums;
using AccessManagement.Domain.Authorization.ValueObjects;
using MediatR;
using Microsoft.EntityFrameworkCore;
using NotFoundException = AccessManagement.Application.Common.Exceptions.NotFoundException;

namespace AccessManagement.Application.Authorization.Queries.GetGrantById;

public record GetGrantByIdQuery(Guid Id) : IRequest<GrantDetailsDto>;

public record GrantDetailsDto(
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
    bool IsActive,
    IReadOnlyList<GrantConstraintItemDto> Constraints);

public record GrantConstraintItemDto(
    string Kind,
    decimal? MaxAmount,
    TimeOnly? TimeFrom,
    TimeOnly? TimeTo,
    string? ScopeKey,
    string? ScopeValue);

public class GetGrantByIdQueryHandler : IRequestHandler<GetGrantByIdQuery, GrantDetailsDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICompanyVisibilityService _visibility;

    public GetGrantByIdQueryHandler(IApplicationDbContext context, ICompanyVisibilityService visibility)
    {
        _context = context;
        _visibility = visibility;
    }

    public async Task<GrantDetailsDto> Handle(GetGrantByIdQuery request, CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var grant = await _context.Grants
            .AsNoTracking()
            .Include(g => g.Permission)
            .Include(g => g.Constraints)
            .SingleOrDefaultAsync(g => g.Id == request.Id, cancellationToken);

        if (grant is null)
        {
            throw new NotFoundException(nameof(Domain.Authorization.Grant), request.Id);
        }

        var vis = await _visibility.ResolveAsync(cancellationToken);
        if (!vis.IsAdmin)
        {
            var visible =
                (grant.SubjectType == SubjectType.Position && vis.PositionIds.Contains(grant.SubjectId))
                || (grant.SubjectUserId is Guid userId && vis.UserIds.Contains(userId))
                || (grant.ScopeUnitId is Guid scope && vis.UnitIds.Contains(scope));
            if (!visible)
            {
                throw new NotFoundException(nameof(Domain.Authorization.Grant), request.Id);
            }
        }

        var constraints = grant.Constraints
            .Select(c => new GrantConstraintItemDto(
                c.Kind.ToString(),
                c.MaxAmount,
                c.TimeFrom,
                c.TimeTo,
                c.ScopeKey,
                c.ScopeValue))
            .ToList();

        return new GrantDetailsDto(
            grant.Id,
            grant.SubjectType,
            grant.SubjectId,
            grant.SubjectUserId,
            grant.PermissionId,
            grant.Permission.Code,
            grant.Resource,
            grant.ResourceId,
            grant.ScopeUnitId,
            grant.Effect,
            grant.SourceType,
            grant.SourceId,
            grant.SourceUserId,
            grant.ValidFrom,
            grant.ValidTo,
            grant.Priority,
            grant.ValidFrom <= now && (grant.ValidTo == null || grant.ValidTo >= now),
            constraints);
    }
}
