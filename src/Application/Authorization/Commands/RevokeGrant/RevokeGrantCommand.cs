using qc_authorization.Application.Authorization.Audit;
using qc_authorization.Application.Common.Interfaces;
using qc_authorization.Application.Common.Interfaces.Repositories;
using MediatR;

namespace qc_authorization.Application.Authorization.Commands.RevokeGrant;

public record RevokeGrantCommand(int GrantId, int? ActorUserId = null) : IRequest;

public class RevokeGrantCommandHandler : IRequestHandler<RevokeGrantCommand>
{
    private readonly IGrantRepository _grants;
    private readonly IAuthorizationAuditService _audit;
    private readonly IUnitOfWork _unitOfWork;

    public RevokeGrantCommandHandler(
        IGrantRepository grants,
        IAuthorizationAuditService audit,
        IUnitOfWork unitOfWork)
    {
        _grants = grants;
        _audit = audit;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(RevokeGrantCommand request, CancellationToken cancellationToken)
    {
        var grant = await _grants.GetByIdAsync(request.GrantId, cancellationToken)
            ?? throw new InvalidOperationException($"Grant {request.GrantId} not found.");

        await _grants.RemoveAsync(grant, cancellationToken);
        await _audit.RecordAsync(
            "GrantRevoked",
            request.ActorUserId,
            $"grantId={grant.Id};subject={grant.SubjectType}:{grant.SubjectId}",
            cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
