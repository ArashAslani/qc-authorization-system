using AccessManagement.Application.Common.Exceptions;
using AccessManagement.Application.Common.Interfaces;
using AccessManagement.Application.Common.Security;
using MediatR;
using Microsoft.EntityFrameworkCore;
using NotFoundException = AccessManagement.Application.Common.Exceptions.NotFoundException;

namespace AccessManagement.Application.Organization.Commands.SetPrimaryPositionAssignment;

public record SetPrimaryPositionAssignmentCommand(Guid PersonnelId, Guid AssignmentId) : IRequest, IRequireUserAdmin;

public class SetPrimaryPositionAssignmentCommandHandler : IRequestHandler<SetPrimaryPositionAssignmentCommand>
{
    private readonly IApplicationDbContext _context;

    public SetPrimaryPositionAssignmentCommandHandler(IApplicationDbContext context) => _context = context;

    public async Task Handle(SetPrimaryPositionAssignmentCommand request, CancellationToken cancellationToken)
    {
        var assignment = await _context.PositionAssignments
            .SingleOrDefaultAsync(a => a.Id == request.AssignmentId && a.PersonnelId == request.PersonnelId, cancellationToken);

        if (assignment is null)
        {
            throw new NotFoundException(nameof(Domain.Organization.PositionAssignment), request.AssignmentId);
        }

        var now = DateTimeOffset.UtcNow;
        if (assignment.ValidFrom > now || (assignment.ValidTo is { } end && end < now))
        {
            throw new InvalidOperationException("Cannot set an inactive assignment as primary.");
        }

        var allAssignments = await _context.PositionAssignments
            .Where(a => a.PersonnelId == request.PersonnelId)
            .ToListAsync(cancellationToken);

        foreach (var a in allAssignments)
        {
            if (a.IsPrimary)
            {
                a.ClearPrimary();
            }
        }

        assignment.MarkAsPrimary();
        await _context.SaveChangesAsync(cancellationToken);
    }
}
