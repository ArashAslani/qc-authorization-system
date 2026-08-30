using AccessManagement.Application.Authorization.Audit;
using AccessManagement.Application.Authorization.Services;
using AccessManagement.Application.Common.Interfaces;
using AccessManagement.Application.Common.Security;
using MediatR;

namespace AccessManagement.Application.Authorization.Commands.AssignRoleGroupToUser;

public record AssignRoleGroupToUserCommand(
    Guid UserId,
    Guid RoleGroupId,
    DateTimeOffset ValidFrom,
    DateTimeOffset? ValidTo = null,
    Guid? ScopeUnitId = null) : IRequest, IRequireUserAdmin;

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
            request.ScopeUnitId,
            cancellationToken);

        await _audit.RecordAsync(
            "RoleGroupAssignedToUser",
            null,
            $"userId={request.UserId};roleGroupId={request.RoleGroupId};grantCount={grantCount}",
            cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
