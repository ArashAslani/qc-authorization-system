using qc_authorization.Application.Common.Exceptions;
using qc_authorization.Application.Common.Interfaces;
using qc_authorization.Domain.Organization.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using NotFoundException = qc_authorization.Application.Common.Exceptions.NotFoundException;

namespace qc_authorization.Application.Organization.Queries.GetPersonnelById;

public record GetPersonnelByIdQuery(Guid Id) : IRequest<PersonnelDetailsDto>;

public record PersonnelDetailsDto(
    Guid Id,
    string NationalId,
    string FirstName,
    string LastName,
    string PersonalCode,
    string? PhoneNumber,
    PersonnelGender Gender,
    PersonnelStatus Status,
    Guid? IdentityUserId,
    IReadOnlyList<PersonnelAssignmentDto> Assignments);

public record PersonnelAssignmentDto(
    Guid Id,
    Guid PositionId,
    string PositionCode,
    string PositionTitle,
    DateTimeOffset ValidFrom,
    DateTimeOffset? ValidTo,
    bool IsActive);

public class GetPersonnelByIdQueryHandler : IRequestHandler<GetPersonnelByIdQuery, PersonnelDetailsDto>
{
    private readonly IApplicationDbContext _context;

    public GetPersonnelByIdQueryHandler(IApplicationDbContext context) => _context = context;

    public async Task<PersonnelDetailsDto> Handle(GetPersonnelByIdQuery request, CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;

        var personnel = await _context.Personnel
            .AsNoTracking()
            .Include(p => p.Assignments)
                .ThenInclude(a => a.Position)
            .SingleOrDefaultAsync(p => p.Id == request.Id, cancellationToken);

        if (personnel is null)
        {
            throw new NotFoundException(nameof(Domain.Organization.Personnel), request.Id);
        }

        var assignments = personnel.Assignments
            .OrderByDescending(a => a.ValidFrom)
            .Select(a => new PersonnelAssignmentDto(
                a.Id,
                a.PositionId,
                a.Position.Code,
                a.Position.Title,
                a.ValidFrom,
                a.ValidTo,
                a.ValidFrom <= now && (a.ValidTo == null || a.ValidTo >= now)))
            .ToList();

        return new PersonnelDetailsDto(
            personnel.Id,
            personnel.NationalId,
            personnel.FirstName,
            personnel.LastName,
            personnel.PersonalCode,
            personnel.PhoneNumber,
            personnel.Gender,
            personnel.Status,
            personnel.IdentityUserId,
            assignments);
    }
}
