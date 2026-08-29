using qc_authorization.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace qc_authorization.Application.Organization.Queries.GetPositionAssignments;

public record GetPositionAssignmentsQuery(
    Guid? PersonnelId = null,
    Guid? PositionId = null,
    bool? ActiveOnly = null) : IRequest<IReadOnlyList<PositionAssignmentDto>>;

public record PositionAssignmentDto(
    Guid Id,
    Guid PersonnelId,
    string PersonnelName,
    string PersonalCode,
    Guid PositionId,
    string PositionCode,
    string PositionTitle,
    DateTimeOffset ValidFrom,
    DateTimeOffset? ValidTo,
    bool IsActive);

public class GetPositionAssignmentsQueryHandler : IRequestHandler<GetPositionAssignmentsQuery, IReadOnlyList<PositionAssignmentDto>>
{
    private readonly IApplicationDbContext _context;

    public GetPositionAssignmentsQueryHandler(IApplicationDbContext context) => _context = context;

    public async Task<IReadOnlyList<PositionAssignmentDto>> Handle(GetPositionAssignmentsQuery request, CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var query = _context.PositionAssignments
            .AsNoTracking()
            .Include(a => a.Personnel)
            .Include(a => a.Position)
            .AsQueryable();

        if (request.PersonnelId.HasValue)
        {
            query = query.Where(a => a.PersonnelId == request.PersonnelId.Value);
        }

        if (request.PositionId.HasValue)
        {
            query = query.Where(a => a.PositionId == request.PositionId.Value);
        }

        if (request.ActiveOnly == true)
        {
            query = query.Where(a => a.ValidFrom <= now && (a.ValidTo == null || a.ValidTo >= now));
        }

        return await query
            .OrderByDescending(a => a.ValidFrom)
            .Select(a => new PositionAssignmentDto(
                a.Id,
                a.PersonnelId,
                $"{a.Personnel.FirstName} {a.Personnel.LastName}",
                a.Personnel.PersonalCode,
                a.PositionId,
                a.Position.Code,
                a.Position.Title,
                a.ValidFrom,
                a.ValidTo,
                a.ValidFrom <= now && (a.ValidTo == null || a.ValidTo >= now)))
            .ToListAsync(cancellationToken);
    }
}
