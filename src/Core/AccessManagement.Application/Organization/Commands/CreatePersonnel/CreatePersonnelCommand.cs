using AccessManagement.Application.Common.Interfaces;
using AccessManagement.Domain.Organization;
using AccessManagement.Domain.Organization.Enums;
using AccessManagement.Application.Common.Security;
using MediatR;

namespace AccessManagement.Application.Organization.Commands.CreatePersonnel;

public record CreatePersonnelCommand(
    string NationalId,
    string FirstName,
    string LastName,
    string PersonalCode,
    string? PhoneNumber = null,
    PersonnelGender Gender = PersonnelGender.Unknown,
    PersonnelStatus Status = PersonnelStatus.Active,
    Guid? IdentityUserId = null) : IRequest<Guid>, IRequireUserAdmin;

public class CreatePersonnelCommandHandler : IRequestHandler<CreatePersonnelCommand, Guid>
{
    private readonly IApplicationDbContext _context;
    private readonly IPersonnelIdentityBridge _personnelIdentityBridge;

    public CreatePersonnelCommandHandler(
        IApplicationDbContext context,
        IPersonnelIdentityBridge personnelIdentityBridge)
    {
        _context = context;
        _personnelIdentityBridge = personnelIdentityBridge;
    }

    public async Task<Guid> Handle(CreatePersonnelCommand request, CancellationToken cancellationToken)
    {
        var personnel = Personnel.Create(
            request.NationalId,
            request.FirstName,
            request.LastName,
            request.PersonalCode,
            request.PhoneNumber,
            request.Gender,
            request.Status,
            request.IdentityUserId);

        _context.Personnel.Add(personnel);
        await _context.SaveChangesAsync(cancellationToken);

        if (request.IdentityUserId.HasValue)
        {
            await _personnelIdentityBridge.LinkAsync(
                personnel.Id,
                request.IdentityUserId.Value,
                cancellationToken);
        }

        return personnel.Id;
    }
}
