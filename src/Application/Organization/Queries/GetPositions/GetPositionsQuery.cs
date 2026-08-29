using qc_authorization.Application.Common.Interfaces;
using qc_authorization.Domain.Authorization.Enums;
using qc_authorization.Domain.Organization.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace qc_authorization.Application.Organization.Queries.GetPositions;

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

    public GetPositionsQueryHandler(IApplicationDbContext context) => _context = context;

    public async Task<IReadOnlyList<PositionDto>> Handle(GetPositionsQuery request, CancellationToken cancellationToken)
    {
        var query = _context.Positions.AsNoTracking().AsQueryable();

        if (request.CompanyId.HasValue)
        {
            query = query.Where(p => p.CompanyId == request.CompanyId.Value);
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
            p.CompanyId,
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
