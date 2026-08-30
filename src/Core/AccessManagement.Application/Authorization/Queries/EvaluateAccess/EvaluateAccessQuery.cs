using AccessManagement.Application.Abstractions;
using AccessManagement.Application.Authorization.Services;
using AccessManagement.Application.Common.Interfaces;
using AccessManagement.Application.Session;
using MediatR;

namespace AccessManagement.Application.Authorization.Queries.EvaluateAccess;

public record EvaluateAccessQuery(
    Guid UserId,
    string PermissionCode,
    Guid? ActivePositionId = null,
    Guid? ResourceScopeUnitId = null,
    DateTimeOffset? When = null) : IRequest<AccessDecisionDto>;

public record AccessDecisionDto(
    bool Allowed,
    string Reason,
    Guid TraceId);

public class EvaluateAccessQueryHandler : IRequestHandler<EvaluateAccessQuery, AccessDecisionDto>
{
    private readonly IAccessEvaluator _evaluator;
    private readonly IActorAccessService _actorAccess;
    private readonly ICurrentUser _currentUser;
    private readonly CompanyWorkspaceService _workspace;

    public EvaluateAccessQueryHandler(
        IAccessEvaluator evaluator,
        IActorAccessService actorAccess,
        ICurrentUser currentUser,
        CompanyWorkspaceService workspace)
    {
        _evaluator = evaluator;
        _actorAccess = actorAccess;
        _currentUser = currentUser;
        _workspace = workspace;
    }

    public async Task<AccessDecisionDto> Handle(EvaluateAccessQuery request, CancellationToken cancellationToken)
    {
        var when = request.When ?? DateTimeOffset.UtcNow;
        AccessManagement.Domain.Authorization.Evaluation.AccessDecision decision;

        if (request.ActivePositionId is Guid positionId)
        {
            decision = await _evaluator.EvaluateAsync(
                new AccessManagement.Domain.Authorization.Evaluation.AccessRequest(
                    request.UserId,
                    positionId,
                    request.PermissionCode,
                    request.ResourceScopeUnitId,
                    when),
                cancellationToken);
        }
        else if (_currentUser.ActiveCompanyId is Guid companyId)
        {
            decision = await _workspace.EvaluateInCompanyAsync(
                request.UserId,
                companyId,
                request.PermissionCode,
                request.ResourceScopeUnitId,
                cancellationToken);
        }
        else
        {
            decision = await _actorAccess.EvaluateAsync(
                request.UserId,
                null,
                request.PermissionCode,
                request.ResourceScopeUnitId,
                cancellationToken);
        }

        return new AccessDecisionDto(decision.Allowed, decision.Reason, decision.TraceId);
    }
}
