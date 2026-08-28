using qc_authorization.Application.Common.Interfaces;
using qc_authorization.Domain.Organization;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace qc_authorization.Application.Organization.Commands.CreatePosition;

public record CreatePositionCommand(
    int CompanyId,
    string Code,
    string Title,
    string? Description,
    int? ParentPositionId) : IRequest<int>;

public class CreatePositionCommandHandler : IRequestHandler<CreatePositionCommand, int>
{
    private readonly IApplicationDbContext _context;
    private readonly PositionHierarchyService _hierarchy;

    public CreatePositionCommandHandler(IApplicationDbContext context, PositionHierarchyService hierarchy)
    {
        _context = context;
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
            var allPositions = await _context.Positions.ToListAsync(cancellationToken);
            var parent = allPositions.FirstOrDefault(p => p.Id == parentId)
                ?? throw new InvalidOperationException($"Parent position {parentId} not found.");

            position.Reparent(parent, allPositions, _hierarchy);
        }

        _context.Positions.Add(position);
        await _context.SaveChangesAsync(cancellationToken);
        return position.Id;
    }
}
