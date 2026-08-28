using qc_authorization.Application.Common.Interfaces;
using qc_authorization.Application.Common.Interfaces.Repositories;
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
    int? SystemUserId = null) : IRequest<int>;

public class CreatePersonnelCommandHandler : IRequestHandler<CreatePersonnelCommand, int>
{
    private readonly IPersonnelRepository _personnel;
    private readonly IUnitOfWork _unitOfWork;

    public CreatePersonnelCommandHandler(IPersonnelRepository personnel, IUnitOfWork unitOfWork)
    {
        _personnel = personnel;
        _unitOfWork = unitOfWork;
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
            request.SystemUserId);

        await _personnel.AddAsync(personnel, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return personnel.Id;
    }
}
