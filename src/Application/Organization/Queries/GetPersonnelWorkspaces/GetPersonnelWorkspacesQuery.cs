using qc_authorization.Application.Common.Exceptions;
using qc_authorization.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using NotFoundException = qc_authorization.Application.Common.Exceptions.NotFoundException;

namespace qc_authorization.Application.Organization.Queries.GetPersonnelWorkspaces;

public record GetPersonnelWorkspacesQuery(Guid PersonnelId, DateTimeOffset? AsOf = null) : IRequest<PersonnelWorkspacesDto>;

public record PersonnelWorkspacesDto(
    Guid PersonnelId,
    Guid? DefaultCompanyId,
    IReadOnlyList<CompanyWorkspaceDto> Companies);

public record CompanyWorkspaceDto(
    Guid CompanyId,
    IReadOnlyList<WorkspacePositionDto> Positions);

public record WorkspacePositionDto(
    Guid AssignmentId,
    Guid PositionId,
    string PositionCode,
    string PositionTitle,
    bool IsPrimary,
    bool IsActive);

public static class CompanyWorkspaceDefaults
{
    public static Guid? ResolveDefaultCompanyId(IReadOnlyList<CompanyWorkspaceDto> companies)
    {
        if (companies.Count == 0)
        {
            return null;
        }

        foreach (var company in companies)
        {
            if (company.Positions.Any(p => p.IsPrimary))
            {
                return company.CompanyId;
            }
        }

        return companies.OrderBy(c => c.CompanyId).First().CompanyId;
    }
}

public class GetPersonnelWorkspacesQueryHandler : IRequestHandler<GetPersonnelWorkspacesQuery, PersonnelWorkspacesDto>
{
    private readonly IApplicationDbContext _context;

    public GetPersonnelWorkspacesQueryHandler(IApplicationDbContext context) => _context = context;

    public async Task<PersonnelWorkspacesDto> Handle(GetPersonnelWorkspacesQuery request, CancellationToken cancellationToken)
    {
        var personnelExists = await _context.Personnel
            .AsNoTracking()
            .AnyAsync(p => p.Id == request.PersonnelId, cancellationToken);

        if (!personnelExists)
        {
            throw new NotFoundException(nameof(Domain.Organization.Personnel), request.PersonnelId);
        }

        var asOf = request.AsOf ?? DateTimeOffset.UtcNow;

        var assignments = await _context.PositionAssignments
            .AsNoTracking()
            .Include(a => a.Position)
            .Where(a => a.PersonnelId == request.PersonnelId
                     && a.ValidFrom <= asOf
                     && (a.ValidTo == null || a.ValidTo >= asOf))
            .OrderBy(a => a.Position.CompanyId)
            .ThenBy(a => a.Position.Code)
            .ToListAsync(cancellationToken);

        var companies = assignments
            .GroupBy(a => a.Position.CompanyId)
            .Select(g => new CompanyWorkspaceDto(
                g.Key,
                g.Select(a => new WorkspacePositionDto(
                    a.Id,
                    a.PositionId,
                    a.Position.Code,
                    a.Position.Title,
                    a.IsPrimary,
                    true)).ToList()))
            .ToList();

        return new PersonnelWorkspacesDto(
            request.PersonnelId,
            CompanyWorkspaceDefaults.ResolveDefaultCompanyId(companies),
            companies);
    }
}
