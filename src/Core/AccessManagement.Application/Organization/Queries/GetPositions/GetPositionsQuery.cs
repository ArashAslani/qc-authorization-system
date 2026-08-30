using AccessManagement.Application.Authorization.Services;
using AccessManagement.Application.Common.Interfaces;
using AccessManagement.Domain.Authorization.Enums;
using AccessManagement.Domain.Organization.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AccessManagement.Application.Organization.Queries.GetPositions;

public record GetPositionsQuery(
    Guid? CompanyId = null,
    string? SearchTerm = null,
    Guid? ParentPositionId = null) : IRequest<IReadOnlyList<PositionDto>>;

public record PositionDto(
    Guid Id,
    Guid CompanyId,
    string Code,
    string Title,
    string? Description,
    Guid? ParentPositionId,
    string? ParentPositionTitle,
    PositionStatus Status,
    int RelatedRoleCount,
    int RelatedRoleGroupCount,
    int AssigneeCount);

public class GetPositionsQueryHandler : IRequestHandler<GetPositionsQuery, IReadOnlyList<PositionDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICompanyVisibilityService _visibility;

    public GetPositionsQueryHandler(IApplicationDbContext context, ICompanyVisibilityService visibility)
    {
        _context = context;
        _visibility = visibility;
    }

    public async Task<IReadOnlyList<PositionDto>> Handle(GetPositionsQuery request, CancellationToken cancellationToken)
    {
        var query = _context.Positions.AsNoTracking().AsQueryable();

        var vis = await _visibility.ResolveAsync(cancellationToken);
        var companyId = request.CompanyId;
        if (!vis.IsAdmin)
        {
            companyId = vis.CompanyUnitId;
        }

        if (companyId.HasValue)
        {
            query = query.Where(p => p.CompanyUnitId == companyId.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var term = request.SearchTerm.Trim().ToLower();
            query = query.Where(p => p.Code.ToLower().Contains(term) || p.Title.ToLower().Contains(term));
        }

        if (request.ParentPositionId.HasValue)
        {
            query = query.Where(p => p.ParentPositionId == request.ParentPositionId.Value);
        }

        var positions = await query.OrderBy(p => p.Code).ToListAsync(cancellationToken);
        var allPositions = await _context.Positions.AsNoTracking().ToDictionaryAsync(p => p.Id, cancellationToken);

        return positions.Select(p => new PositionDto(
            p.Id,
            p.CompanyUnitId,
            p.Code,
            p.Title,
            p.Description,
            p.ParentPositionId,
            p.ParentPositionId.HasValue && allPositions.TryGetValue(p.ParentPositionId.Value, out var parent)
                ? parent.Title
                : null,
            p.Status,
            _context.Grants
                .Where(g => g.SubjectType == SubjectType.Position
                         && g.SubjectId == p.Id
                         && g.SourceType == SourceType.Role)
                .Select(g => g.SourceId)
                .Distinct()
                .Count(),
            _context.Grants
                .Where(g => g.SubjectType == SubjectType.Position
                         && g.SubjectId == p.Id
                         && g.SourceType == SourceType.RoleGroup)
                .Select(g => g.SourceId)
                .Distinct()
                .Count(),
            _context.PositionAssignments.Count(a => a.PositionId == p.Id))).ToList();
    }
}
