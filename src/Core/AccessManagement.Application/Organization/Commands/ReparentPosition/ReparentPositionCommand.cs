using AccessManagement.Application.Common.Interfaces;
using AccessManagement.Domain.Organization;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AccessManagement.Application.Organization.Commands.ReparentPosition;

public record ReparentPositionCommand(Guid PositionId, Guid? NewParentPositionId) : IRequest;

public class ReparentPositionCommandHandler : IRequestHandler<ReparentPositionCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly PositionHierarchyService _hierarchy;

    public ReparentPositionCommandHandler(IApplicationDbContext context, PositionHierarchyService hierarchy)
    {
        _context = context;
        _hierarchy = hierarchy;
    }

    public async Task Handle(ReparentPositionCommand request, CancellationToken cancellationToken)
    {
        var all = await _context.Positions.ToListAsync(cancellationToken);

        var position = all.FirstOrDefault(p => p.Id == request.PositionId)
            ?? throw new InvalidOperationException($"Position {request.PositionId} not found.");

        Position? newParent = null;
        if (request.NewParentPositionId is Guid parentId)
        {
            newParent = all.FirstOrDefault(p => p.Id == parentId)
                ?? throw new InvalidOperationException($"Parent position {parentId} not found.");
        }

        position.Reparent(newParent, all, _hierarchy);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
