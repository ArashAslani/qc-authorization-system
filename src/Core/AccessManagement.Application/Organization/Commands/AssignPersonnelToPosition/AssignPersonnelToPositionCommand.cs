using AccessManagement.Application.Common.Interfaces;
using AccessManagement.Domain.Organization;
using AccessManagement.Application.Common.Security;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AccessManagement.Application.Organization.Commands.AssignPersonnelToPosition;

public record AssignPersonnelToPositionCommand(
    Guid PersonnelId,
    Guid PositionId,
    DateTimeOffset ValidFrom,
    DateTimeOffset? ValidTo = null) : IRequest<Guid>, IRequireUserAdmin;

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

        var position = await _context.Positions
            .FirstOrDefaultAsync(p => p.Id == request.PositionId, cancellationToken)
            ?? throw new InvalidOperationException($"Position {request.PositionId} not found.");

        var overlapStart = request.ValidFrom;
        var active = await _context.PositionAssignments
            .Include(a => a.Position)
            .Where(a => a.PersonnelId == request.PersonnelId
                     && a.Position.CompanyUnitId == position.CompanyUnitId
                     && a.ValidFrom <= overlapStart
                     && (a.ValidTo == null || a.ValidTo > overlapStart))
            .ToListAsync(cancellationToken);

        foreach (var existing in active)
        {
            existing.End(overlapStart);
        }

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
