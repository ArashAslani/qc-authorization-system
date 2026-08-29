using qc_authorization.Application.Common.Interfaces;
using qc_authorization.Domain.Organization.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace qc_authorization.Application.Organization.Queries.GetPersonnel;

public record GetPersonnelQuery(
    string? SearchTerm = null,
    PersonnelStatus? Status = null,
    bool? HasIdentityUser = null) : IRequest<IReadOnlyList<PersonnelDto>>;

public record PersonnelDto(
    Guid Id,
    string NationalId,
    string FirstName,
    string LastName,
    string PersonalCode,
    string? PhoneNumber,
    PersonnelGender Gender,
    PersonnelStatus Status,
    Guid? IdentityUserId);

public class GetPersonnelQueryHandler : IRequestHandler<GetPersonnelQuery, IReadOnlyList<PersonnelDto>>
{
    private readonly IApplicationDbContext _context;

    public GetPersonnelQueryHandler(IApplicationDbContext context) => _context = context;

    public async Task<IReadOnlyList<PersonnelDto>> Handle(GetPersonnelQuery request, CancellationToken cancellationToken)
    {
        var query = _context.Personnel.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var term = request.SearchTerm.Trim().ToLower();
            query = query.Where(p =>
                p.FirstName.ToLower().Contains(term) ||
                p.LastName.ToLower().Contains(term) ||
                p.NationalId.Contains(term) ||
                p.PersonalCode.Contains(term));
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
                p.PersonalCode,
                p.PhoneNumber,
                p.Gender,
                p.Status,
                p.IdentityUserId))
            .ToListAsync(cancellationToken);
    }
}
