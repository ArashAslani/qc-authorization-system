using AccessManagement.Application.Common.Exceptions;
using AccessManagement.Application.Common.Interfaces;
using AccessManagement.Domain.Authorization.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using NotFoundException = AccessManagement.Application.Common.Exceptions.NotFoundException;

namespace AccessManagement.Application.Organization.Queries.GetPositionAuthorizationSummary;

public record GetPositionAuthorizationSummaryQuery(Guid PositionId) : IRequest<PositionAuthorizationSummaryDto>;

public record PositionAuthorizationSummaryDto(
    Guid PositionId,
    IReadOnlyList<PositionRoleSummaryDto> Roles,
    IReadOnlyList<PositionRoleGroupSummaryDto> RoleGroups);

public record PositionRoleSummaryDto(Guid RoleId, string Code, string Name, CatalogStatus Status);

public record PositionRoleGroupSummaryDto(Guid RoleGroupId, string Code, string Name, CatalogStatus Status);

public class GetPositionAuthorizationSummaryQueryHandler
    : IRequestHandler<GetPositionAuthorizationSummaryQuery, PositionAuthorizationSummaryDto>
{
    private readonly IApplicationDbContext _context;

    public GetPositionAuthorizationSummaryQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PositionAuthorizationSummaryDto> Handle(
        GetPositionAuthorizationSummaryQuery request,
        CancellationToken cancellationToken)
    {
        var positionExists = await _context.Positions
            .AsNoTracking()
            .AnyAsync(p => p.Id == request.PositionId, cancellationToken);

        if (!positionExists)
        {
            throw new NotFoundException(nameof(Domain.Organization.Position), request.PositionId);
        }

        var roleSourceIds = await _context.Grants
            .AsNoTracking()
            .Where(g => g.SubjectType == SubjectType.Position
                     && g.SubjectId == request.PositionId
                     && g.SourceType == SourceType.Role)
            .Select(g => g.SourceId)
            .Distinct()
            .ToListAsync(cancellationToken);

        var roleGroupSourceIds = await _context.Grants
            .AsNoTracking()
            .Where(g => g.SubjectType == SubjectType.Position
                     && g.SubjectId == request.PositionId
                     && g.SourceType == SourceType.RoleGroup)
            .Select(g => g.SourceId)
            .Distinct()
            .ToListAsync(cancellationToken);

        var roles = roleSourceIds.Count == 0
            ? []
            : await _context.AuthorizationRoles
                .AsNoTracking()
                .Where(r => roleSourceIds.Contains(r.Id))
                .OrderBy(r => r.Code)
                .Select(r => new PositionRoleSummaryDto(r.Id, r.Code, r.Name, r.Status))
                .ToListAsync(cancellationToken);

        var roleGroups = roleGroupSourceIds.Count == 0
            ? []
            : await _context.RoleGroups
                .AsNoTracking()
                .Where(rg => roleGroupSourceIds.Contains(rg.Id))
                .OrderBy(rg => rg.Code)
                .Select(rg => new PositionRoleGroupSummaryDto(rg.Id, rg.Code, rg.Name, rg.Status))
                .ToListAsync(cancellationToken);

        return new PositionAuthorizationSummaryDto(request.PositionId, roles, roleGroups);
    }
}
