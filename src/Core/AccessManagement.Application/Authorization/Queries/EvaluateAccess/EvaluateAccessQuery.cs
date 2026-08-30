using AccessManagement.Application.Abstractions;
using AccessManagement.Application.Common.Interfaces;
using AccessManagement.Domain.Authorization.Enums;
using AccessManagement.Domain.Authorization.Evaluation;
using MediatR;

namespace AccessManagement.Application.Authorization.Queries.EvaluateAccess;

public record EvaluateAccessQuery(
    SubjectType SubjectType,
    Guid SubjectId,
    Guid? UserId,
    string Action,
    string Resource,
    string? ResourceId,
    DateTimeOffset When,
    Guid? ActivePositionId = null,
    Guid? ResourceScopeUnitId = null) : IRequest<AccessDecisionDto>;

public record AccessDecisionDto(
    bool Allowed,
    string Reason,
    Guid TraceId);

public class EvaluateAccessQueryHandler : IRequestHandler<EvaluateAccessQuery, AccessDecisionDto>
{
    private readonly IAccessEvaluator _evaluator;

    public EvaluateAccessQueryHandler(IAccessEvaluator evaluator) => _evaluator = evaluator;

    public async Task<AccessDecisionDto> Handle(EvaluateAccessQuery request, CancellationToken cancellationToken)
    {
        var userId = request.UserId ?? (request.SubjectType == SubjectType.User ? request.SubjectId : Guid.Empty);
        var positionId = request.ActivePositionId
            ?? (request.SubjectType == SubjectType.Position ? request.SubjectId : null);

        var accessRequest = AccessRequest.ForUser(
            userId,
            request.Action,
            request.Resource,
            request.ResourceId,
            request.When,
            positionId,
            request.ResourceScopeUnitId);

        var decision = await _evaluator.EvaluateAsync(accessRequest, cancellationToken);
        return new AccessDecisionDto(decision.Allowed, decision.Reason, decision.TraceId);
    }
}
