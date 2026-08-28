using qc_authorization.Application.Common.Interfaces;
using qc_authorization.Domain.Organization;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace qc_authorization.Application.Organization.Commands.AssignPersonnelToPosition;

public record AssignPersonnelToPositionCommand(
    int PersonnelId,
    int PositionId,
    DateTimeOffset ValidFrom,
    DateTimeOffset? ValidTo = null) : IRequest<int>;

public class AssignPersonnelToPositionCommandHandler : IRequestHandler<AssignPersonnelToPositionCommand, int>
{
    private readonly IApplicationDbContext _context;

    public AssignPersonnelToPositionCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<int> Handle(AssignPersonnelToPositionCommand request, CancellationToken cancellationToken)
    {
        _ = await _context.Personnel
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == request.PersonnelId, cancellationToken)
            ?? throw new InvalidOperationException($"Personnel {request.PersonnelId} not found.");

        _ = await _context.Positions
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == request.PositionId, cancellationToken)
            ?? throw new InvalidOperationException($"Position {request.PositionId} not found.");

        var assignment = PositionAssignment.Create(
            request.PersonnelId,
            request.PositionId,
            request.ValidFrom,
            request.ValidTo);

        _context.PositionAssignments.Add(assignment);
        await _context.SaveChangesAsync(cancellationToken);
        return assignment.Id;
    }
}
