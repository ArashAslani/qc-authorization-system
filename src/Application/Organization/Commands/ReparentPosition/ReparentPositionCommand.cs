using qc_authorization.Application.Common.Interfaces;
using qc_authorization.Application.Common.Interfaces.Repositories;
using qc_authorization.Domain.Organization;
using MediatR;

namespace qc_authorization.Application.Organization.Commands.ReparentPosition;

public record ReparentPositionCommand(int PositionId, int? NewParentPositionId) : IRequest;

public class ReparentPositionCommandHandler : IRequestHandler<ReparentPositionCommand>
{
    private readonly IPositionRepository _positions;
    private readonly IUnitOfWork _unitOfWork;
    private readonly PositionHierarchyService _hierarchy;

    public ReparentPositionCommandHandler(
        IPositionRepository positions,
        IUnitOfWork unitOfWork,
        PositionHierarchyService hierarchy)
    {
        _positions = positions;
        _unitOfWork = unitOfWork;
        _hierarchy = hierarchy;
    }

    public async Task Handle(ReparentPositionCommand request, CancellationToken cancellationToken)
    {
        var position = await _positions.GetByIdAsync(request.PositionId, cancellationToken)
            ?? throw new InvalidOperationException($"Position {request.PositionId} not found.");

        var all = await _positions.GetAllAsync(cancellationToken);
        Position? newParent = null;
        if (request.NewParentPositionId is int parentId)
        {
            newParent = await _positions.GetByIdAsync(parentId, cancellationToken)
                ?? throw new InvalidOperationException($"Parent position {parentId} not found.");
        }

        position.Reparent(newParent, all, _hierarchy);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
