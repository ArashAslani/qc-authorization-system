using qc_authorization.Application.Authorization.Audit;
using qc_authorization.Application.Authorization.Services;
using qc_authorization.Application.Common.Interfaces;
using MediatR;

namespace qc_authorization.Application.Authorization.Commands.AssignRoleGroupToUser;

public record AssignRoleGroupToUserCommand(
    Guid UserId,
    Guid RoleGroupId,
    DateTimeOffset ValidFrom,
    DateTimeOffset? ValidTo = null) : IRequest;

public class AssignRoleGroupToUserCommandHandler : IRequestHandler<AssignRoleGroupToUserCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly IAuthorizationAuditService _audit;
    private readonly RoleGroupGrantMaterializer _materializer;

    public AssignRoleGroupToUserCommandHandler(
        IApplicationDbContext context,
        IAuthorizationAuditService audit,
        RoleGroupGrantMaterializer materializer)
    {
        _context = context;
        _audit = audit;
        _materializer = materializer;
    }

    public async Task Handle(AssignRoleGroupToUserCommand request, CancellationToken cancellationToken)
    {
        var grantCount = await _materializer.MaterializeForUserAsync(
            request.UserId,
            request.RoleGroupId,
            request.ValidFrom,
            request.ValidTo,
            cancellationToken);

        await _audit.RecordAsync(
            "RoleGroupAssignedToUser",
            null,
            $"userId={request.UserId};roleGroupId={request.RoleGroupId};grantCount={grantCount}",
            cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
