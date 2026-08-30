using AccessManagement.Application.Authorization.Services;
using AccessManagement.Application.Common.Interfaces;
using AccessManagement.Domain.Organization.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AccessManagement.Application.Organization.Queries.GetPersonnel;

public record GetPersonnelQuery(
    string? SearchTerm = null,
    PersonnelStatus? Status = null,
    bool? HasIdentityUser = null) : IRequest<IReadOnlyList<PersonnelDto>>;

public record PersonnelDto(
    Guid Id,
    string? NationalId,
    string FirstName,
    string LastName,
    string? PersonalCode,
    string? PhoneNumber,
    PersonnelGender Gender,
    PersonnelStatus Status,
    Guid? IdentityUserId);

public class GetPersonnelQueryHandler : IRequestHandler<GetPersonnelQuery, IReadOnlyList<PersonnelDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICompanyVisibilityService _visibility;

    public GetPersonnelQueryHandler(IApplicationDbContext context, ICompanyVisibilityService visibility)
    {
        _context = context;
        _visibility = visibility;
    }

    public async Task<IReadOnlyList<PersonnelDto>> Handle(GetPersonnelQuery request, CancellationToken cancellationToken)
    {
        var query = _context.Personnel.AsNoTracking().AsQueryable();

        var vis = await _visibility.ResolveAsync(cancellationToken);
        if (!vis.IsAdmin)
        {
            var personnelIds = vis.PersonnelIds.ToList();
            query = query.Where(p => personnelIds.Contains(p.Id));
        }

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var term = request.SearchTerm.Trim().ToLower();
            query = query.Where(p =>
                p.FirstName.ToLower().Contains(term) ||
                p.LastName.ToLower().Contains(term) ||
                (p.NationalId != null && p.NationalId.Contains(term)) ||
                (p.PersonnelCode != null && p.PersonnelCode.Contains(term)));
        }

        if (request.Status.HasValue)
        {
            query = query.Where(p => p.Status == request.Status.Value);
        }

        if (request.HasIdentityUser.HasValue)
        {
            query = request.HasIdentityUser.Value
                ? query.Where(p => p.IdentityUserId != null)
                : query.Where(p => p.IdentityUserId == null);
        }

        return await query
            .OrderBy(p => p.LastName)
            .ThenBy(p => p.FirstName)
            .Select(p => new PersonnelDto(
                p.Id,
                p.NationalId,
                p.FirstName,
                p.LastName,
                p.PersonnelCode,
                p.PhoneNumber,
                p.Gender,
                p.Status,
                p.IdentityUserId))
            .ToListAsync(cancellationToken);
    }
}
