using qc_authorization.Application.Common.Interfaces;
using qc_authorization.Domain.Organization;
using MediatR;

namespace qc_authorization.Application.Organization.Commands.CreatePosition;

public record CreatePositionCommand(string Code, string Name, int? ParentId) : IRequest<int>;

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
        var position = new Position
        {
            Code = request.Code,
            Name = request.Name,
            ParentId = request.ParentId,
        };

        if (request.ParentId is int parentId)
        {
            var parent = await _context.Positions.FindAsync(new object?[] { parentId }, cancellationToken)
                ?? throw new InvalidOperationException($"Parent position {parentId} not found.");
            _hierarchy.EnsureValidParenting(position, parent, await LoadAll(cancellationToken));
        }

        _context.Positions.Add(position);
        await _context.SaveChangesAsync(cancellationToken);
        return position.Id;
    }

    private async Task<List<Position>> LoadAll(CancellationToken ct) =>
        await _context.Positions.AsNoTracking().ToListAsync(ct);
}
