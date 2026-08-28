using qc_authorization.Application.Authorization.Evaluation;
using qc_authorization.Domain.Authorization.Enums;
using qc_authorization.Domain.Authorization.Evaluation;
using Mapster;
using MediatR;

namespace qc_authorization.Application.Authorization.Queries.EvaluateAccess;

public record EvaluateAccessQuery(
    SubjectType SubjectType,
    int SubjectId,
    Guid? UserId,
    string Action,
    string Resource,
    string? ResourceId,
    DateTimeOffset When) : IRequest<AccessDecisionDto>;

public record AccessDecisionDto(
    string Effect,
    string Reason,
    AccessDecisionTraceDto Trace);

public record AccessDecisionTraceDto(
    string TraceId,
    string RequestedPermission,
    string FinalDecision,
    string Reason,
    int CandidateCount,
    int ApplicableCount);

public class EvaluateAccessQueryHandler : IRequestHandler<EvaluateAccessQuery, AccessDecisionDto>
{
    private readonly IAccessEvaluator _evaluator;

    public EvaluateAccessQueryHandler(IAccessEvaluator evaluator)
    {
        _evaluator = evaluator;
    }

    public async Task<AccessDecisionDto> Handle(EvaluateAccessQuery request, CancellationToken cancellationToken)
    {
        var accessRequest = new AccessRequest(
            request.SubjectType,
            request.SubjectId,
            request.UserId,
            request.Action,
            request.Resource,
            request.ResourceId,
            request.When);

        var decision = await _evaluator.EvaluateAsync(accessRequest, cancellationToken);
        return decision.Adapt<AccessDecisionDto>();
    }
}
