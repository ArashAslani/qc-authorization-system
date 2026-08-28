using qc_authorization.Application.Common.Interfaces;
using qc_authorization.Application.Common.Interfaces.Repositories;
using qc_authorization.Domain.Organization;
using MediatR;

namespace qc_authorization.Application.Organization.Commands.AssignPersonnelToPosition;

public record AssignPersonnelToPositionCommand(
    int PersonnelId,
    int PositionId,
    DateTimeOffset ValidFrom,
    DateTimeOffset? ValidTo = null) : IRequest<int>;

public class AssignPersonnelToPositionCommandHandler : IRequestHandler<AssignPersonnelToPositionCommand, int>
{
    private readonly IPersonnelRepository _personnel;
    private readonly IPositionRepository _positions;
    private readonly IPositionAssignmentRepository _assignments;
    private readonly IUnitOfWork _unitOfWork;

    public AssignPersonnelToPositionCommandHandler(
        IPersonnelRepository personnel,
        IPositionRepository positions,
        IPositionAssignmentRepository assignments,
        IUnitOfWork unitOfWork)
    {
        _personnel = personnel;
        _positions = positions;
        _assignments = assignments;
        _unitOfWork = unitOfWork;
    }

    public async Task<int> Handle(AssignPersonnelToPositionCommand request, CancellationToken cancellationToken)
    {
        _ = await _personnel.GetByIdAsync(request.PersonnelId, cancellationToken)
            ?? throw new InvalidOperationException($"Personnel {request.PersonnelId} not found.");

        var position = await _positions.GetByIdAsync(request.PositionId, cancellationToken)
            ?? throw new InvalidOperationException($"Position {request.PositionId} not found.");

        var assignment = PositionAssignment.Create(
            request.PersonnelId,
            request.PositionId,
            request.ValidFrom,
            request.ValidTo);

        await _assignments.AddAsync(assignment, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return assignment.Id;
    }
}
