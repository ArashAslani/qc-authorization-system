using AccessManagement.Application.Authorization.Services;
using AccessManagement.Application.Common.Exceptions;
using AccessManagement.Application.Common.Interfaces;
using AccessManagement.Domain.Organization.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using NotFoundException = AccessManagement.Application.Common.Exceptions.NotFoundException;

namespace AccessManagement.Application.Organization.Queries.GetPersonnelById;

public record GetPersonnelByIdQuery(Guid Id) : IRequest<PersonnelDetailsDto>;

public record PersonnelDetailsDto(
    Guid Id,
    string? NationalId,
    string FirstName,
    string LastName,
    string? PersonalCode,
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
    private readonly ICompanyVisibilityService _visibility;
    private readonly ICurrentUser _currentUser;

    public GetPersonnelByIdQueryHandler(
        IApplicationDbContext context,
        ICompanyVisibilityService visibility,
        ICurrentUser currentUser)
    {
        _context = context;
        _visibility = visibility;
        _currentUser = currentUser;
    }

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

        var vis = await _visibility.ResolveAsync(cancellationToken);
        if (!vis.IsAdmin && !vis.PersonnelIds.Contains(personnel.Id))
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

        var revealPii = vis.IsAdmin || (_currentUser.PersonnelId is Guid self && self == personnel.Id);

        return new PersonnelDetailsDto(
            personnel.Id,
            revealPii ? personnel.NationalId : null,
            personnel.FirstName,
            personnel.LastName,
            personnel.PersonnelCode,
            revealPii ? personnel.PhoneNumber : null,
            personnel.Gender,
            personnel.Status,
            personnel.IdentityUserId,
            assignments);
    }
}
