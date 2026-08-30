using AccessManagement.Application.Common.Interfaces;
using AccessManagement.Application.Common.Security;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AccessManagement.Application.Organization.Commands.EndPositionAssignment;

public record EndPositionAssignmentCommand(Guid AssignmentId, DateTimeOffset? EndedAt = null) : IRequest, IRequireUserAdmin;

public class EndPositionAssignmentCommandHandler : IRequestHandler<EndPositionAssignmentCommand>
{
    private readonly IApplicationDbContext _context;

    public EndPositionAssignmentCommandHandler(IApplicationDbContext context) => _context = context;

    public async Task Handle(EndPositionAssignmentCommand request, CancellationToken cancellationToken)
    {
        var assignment = await _context.PositionAssignments
            .FirstOrDefaultAsync(a => a.Id == request.AssignmentId, cancellationToken)
            ?? throw new InvalidOperationException($"Assignment {request.AssignmentId} was not found.");

        assignment.End(request.EndedAt ?? DateTimeOffset.UtcNow);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
