using AccessManagement.Application.Common.Interfaces;
using AccessManagement.Domain.Organization;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AccessManagement.Application.Organization.Commands.AssignPersonnelToPosition;

public record AssignPersonnelToPositionCommand(
    Guid PersonnelId,
    Guid PositionId,
    DateTimeOffset ValidFrom,
    DateTimeOffset? ValidTo = null) : IRequest<Guid>;

public class AssignPersonnelToPositionCommandHandler : IRequestHandler<AssignPersonnelToPositionCommand, Guid>
{
    private readonly IApplicationDbContext _context;

    public AssignPersonnelToPositionCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Guid> Handle(AssignPersonnelToPositionCommand request, CancellationToken cancellationToken)
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
