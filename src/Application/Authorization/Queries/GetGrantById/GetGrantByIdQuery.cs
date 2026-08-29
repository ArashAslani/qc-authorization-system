using qc_authorization.Application.Common.Exceptions;
using qc_authorization.Application.Common.Interfaces;
using qc_authorization.Domain.Authorization.Enums;
using qc_authorization.Domain.Authorization.ValueObjects;
using MediatR;
using Microsoft.EntityFrameworkCore;
using NotFoundException = qc_authorization.Application.Common.Exceptions.NotFoundException;

namespace qc_authorization.Application.Authorization.Queries.GetGrantById;

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
    ScopeKind ScopeKind,
    string? ScopeIdentifier,
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

    public GetGrantByIdQueryHandler(IApplicationDbContext context) => _context = context;

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
            grant.ScopeKind,
            grant.ScopeIdentifier,
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
