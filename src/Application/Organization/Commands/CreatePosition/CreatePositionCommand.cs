using qc_authorization.Application.Common.Interfaces;
using qc_authorization.Application.Common.Interfaces.Repositories;
using qc_authorization.Domain.Organization;
using MediatR;

namespace qc_authorization.Application.Organization.Commands.CreatePosition;

public record CreatePositionCommand(
    int CompanyId,
    string Code,
    string Title,
    string? Description,
    int? ParentPositionId) : IRequest<int>;

public class CreatePositionCommandHandler : IRequestHandler<CreatePositionCommand, int>
{
    private readonly IPositionRepository _positions;
    private readonly IUnitOfWork _unitOfWork;
    private readonly PositionHierarchyService _hierarchy;

    public CreatePositionCommandHandler(
        IPositionRepository positions,
        IUnitOfWork unitOfWork,
        PositionHierarchyService hierarchy)
    {
        _positions = positions;
        _unitOfWork = unitOfWork;
        _hierarchy = hierarchy;
    }

    public async Task<int> Handle(CreatePositionCommand request, CancellationToken cancellationToken)
    {
        var position = Position.Create(
            request.CompanyId,
            request.Code,
            request.Title,
            request.Description,
            request.ParentPositionId);

        if (request.ParentPositionId is int parentId)
        {
            var parent = await _positions.GetByIdAsync(parentId, cancellationToken)
                ?? throw new InvalidOperationException($"Parent position {parentId} not found.");

            position.Reparent(parent, await _positions.GetAllAsync(cancellationToken), _hierarchy);
        }

        await _positions.AddAsync(position, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return position.Id;
    }
}
