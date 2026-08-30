using AccessManagement.Application.Abstractions;
using AccessManagement.Domain.Authorization.Enums;
using AccessManagement.Domain.Authorization.Evaluation;
using MediatR;

namespace AccessManagement.Application.Authorization.Queries.EvaluateAccessForSubject;

public record EvaluateAccessForSubjectQuery(
    SubjectType SubjectType,
    Guid SubjectId,
    Guid? UserId,
    string Action,
    string Resource,
    string? ResourceId,
    DateTimeOffset When) : IRequest<AdminAccessDecisionDto>;

public record AdminAccessDecisionDto(
    bool Allowed,
    string Reason,
    Guid TraceId);

public class EvaluateAccessForSubjectQueryHandler : IRequestHandler<EvaluateAccessForSubjectQuery, AdminAccessDecisionDto>
{
    private readonly IAccessEvaluator _evaluator;

    public EvaluateAccessForSubjectQueryHandler(IAccessEvaluator evaluator) => _evaluator = evaluator;

    public async Task<AdminAccessDecisionDto> Handle(EvaluateAccessForSubjectQuery request, CancellationToken cancellationToken)
    {
        var userId = request.UserId ?? (request.SubjectType == SubjectType.User ? request.SubjectId : Guid.Empty);
        var positionId = request.SubjectType == SubjectType.Position ? request.SubjectId : (Guid?)null;

        var accessRequest = AccessRequest.ForUser(
            userId,
            request.Action,
            request.Resource,
            request.ResourceId,
            request.When,
            positionId);

        var decision = await _evaluator.EvaluateAsync(accessRequest, cancellationToken);
        return new AdminAccessDecisionDto(decision.Allowed, decision.Reason, decision.TraceId);
    }
}
