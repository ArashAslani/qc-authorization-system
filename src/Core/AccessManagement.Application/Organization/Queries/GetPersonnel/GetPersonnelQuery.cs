using AccessManagement.Application.Authorization.Services;
using AccessManagement.Application.Common.Interfaces;
using AccessManagement.Application.Common.Models;
using AccessManagement.Domain.Organization.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AccessManagement.Application.Organization.Queries.GetPersonnel;

public record GetPersonnelQuery(
    string? SearchTerm = null,
    PersonnelStatus? Status = null,
    bool? HasIdentityUser = null,
    int PageNumber = 1,
    int PageSize = PaginatedList<PersonnelDto>.DefaultPageSize) : IRequest<PaginatedList<PersonnelDto>>;

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

public class GetPersonnelQueryHandler : IRequestHandler<GetPersonnelQuery, PaginatedList<PersonnelDto>>
{
    private const int MaxSearchTermLength = 100;
    private readonly IApplicationDbContext _context;
    private readonly ICompanyVisibilityService _visibility;
    private readonly ICurrentUser _currentUser;

    public GetPersonnelQueryHandler(
        IApplicationDbContext context,
        ICompanyVisibilityService visibility,
        ICurrentUser currentUser)
    {
        _context = context;
        _visibility = visibility;
        _currentUser = currentUser;
    }

    public async Task<PaginatedList<PersonnelDto>> Handle(GetPersonnelQuery request, CancellationToken cancellationToken)
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
            var term = request.SearchTerm.Trim();
            if (term.Length > MaxSearchTermLength)
            {
                term = term[..MaxSearchTermLength];
            }

            term = term.ToLower();
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

        var (pageNumber, pageSize) = PaginatedList<PersonnelDto>.Normalize(request.PageNumber, request.PageSize);
        var totalCount = await query.CountAsync(cancellationToken);
        var revealPii = vis.IsAdmin;
        var selfPersonnelId = _currentUser.PersonnelId;

        var items = await query
            .OrderBy(p => p.LastName)
            .ThenBy(p => p.FirstName)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new PersonnelDto(
                p.Id,
                revealPii || p.Id == selfPersonnelId ? p.NationalId : null,
                p.FirstName,
                p.LastName,
                p.PersonnelCode,
                revealPii || p.Id == selfPersonnelId ? p.PhoneNumber : null,
                p.Gender,
                p.Status,
                p.IdentityUserId))
            .ToListAsync(cancellationToken);

        return new PaginatedList<PersonnelDto>(items, totalCount, pageNumber, pageSize);
    }
}
