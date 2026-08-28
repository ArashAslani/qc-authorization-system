using qc_authorization.Application.Common.Interfaces;
using qc_authorization.Domain.Organization;
using qc_authorization.Domain.Organization.Enums;
using MediatR;

namespace qc_authorization.Application.Organization.Commands.CreatePersonnel;

public record CreatePersonnelCommand(
    string NationalId,
    string FirstName,
    string LastName,
    string PersonalCode,
    string? PhoneNumber = null,
    PersonnelGender Gender = PersonnelGender.Unknown,
    PersonnelStatus Status = PersonnelStatus.Active,
    Guid? IdentityUserId = null) : IRequest<int>;

public class CreatePersonnelCommandHandler : IRequestHandler<CreatePersonnelCommand, int>
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

    public async Task<int> Handle(CreatePersonnelCommand request, CancellationToken cancellationToken)
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
