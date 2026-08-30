using AccessManagement.Application.Authorization.Services;
using AccessManagement.Application.Common.Exceptions;
using AccessManagement.Application.Common.Interfaces;
using AccessManagement.Domain.Organization.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using NotFoundException = AccessManagement.Application.Common.Exceptions.NotFoundException;

namespace AccessManagement.Application.Organization.Queries.GetPositionById;

public record GetPositionByIdQuery(Guid Id) : IRequest<PositionDetailsDto>;

public record PositionDetailsDto(
    Guid Id,
    Guid CompanyId,
    string Code,
    string Title,
    string? Description,
    Guid? ParentPositionId,
    string? ParentPositionTitle,
    PositionStatus Status,
    IReadOnlyList<PositionChildDto> Children,
    IReadOnlyList<PositionAssigneeDto> ActiveAssignees);

public record PositionChildDto(Guid Id, string Code, string Title);

public record PositionAssigneeDto(Guid AssignmentId, Guid PersonnelId, string FullName, string? PersonalCode, DateTimeOffset ValidFrom);

public class GetPositionByIdQueryHandler : IRequestHandler<GetPositionByIdQuery, PositionDetailsDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICompanyVisibilityService _visibility;

    public GetPositionByIdQueryHandler(IApplicationDbContext context, ICompanyVisibilityService visibility)
    {
        _context = context;
        _visibility = visibility;
    }

    public async Task<PositionDetailsDto> Handle(GetPositionByIdQuery request, CancellationToken cancellationToken)
    {
        var position = await _context.Positions
            .AsNoTracking()
            .SingleOrDefaultAsync(p => p.Id == request.Id, cancellationToken);

        if (position is null)
        {
            throw new NotFoundException(nameof(Domain.Organization.Position), request.Id);
        }

        var vis = await _visibility.ResolveAsync(cancellationToken);
        if (!vis.IsAdmin && !vis.PositionIds.Contains(position.Id))
        {
            throw new NotFoundException(nameof(Domain.Organization.Position), request.Id);
        }

        string? parentTitle = null;
        if (position.ParentPositionId.HasValue)
        {
            var parent = await _context.Positions.AsNoTracking()
                .SingleOrDefaultAsync(p => p.Id == position.ParentPositionId.Value, cancellationToken);
            parentTitle = parent?.Title;
        }

        var children = await _context.Positions
            .AsNoTracking()
            .Where(p => p.ParentPositionId == position.Id)
            .OrderBy(p => p.Code)
            .Select(p => new PositionChildDto(p.Id, p.Code, p.Title))
            .ToListAsync(cancellationToken);

        var now = DateTimeOffset.UtcNow;
        var assignees = await _context.PositionAssignments
            .AsNoTracking()
            .Include(a => a.Personnel)
            .Where(a => a.PositionId == position.Id
                     && a.ValidFrom <= now
                     && (a.ValidTo == null || a.ValidTo >= now))
            .Select(a => new PositionAssigneeDto(
                a.Id,
                a.PersonnelId,
                $"{a.Personnel.FirstName} {a.Personnel.LastName}",
                a.Personnel.PersonnelCode,
                a.ValidFrom))
            .ToListAsync(cancellationToken);

        return new PositionDetailsDto(
            position.Id,
            position.CompanyUnitId,
            position.Code,
            position.Title,
            position.Description,
            position.ParentPositionId,
            parentTitle,
            position.Status,
            children,
            assignees);
    }
}
