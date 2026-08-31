using AccessManagement.Application.Abstractions;
using AccessManagement.Application.Common.Security;
using AccessManagement.Domain.Authorization.Evaluation;
using MediatR;

namespace AccessManagement.Application.Authorization.Queries.EvaluateAccessForSubject;

public record EvaluateAccessForSubjectQuery(
    Guid UserId,
    string PermissionCode,
    Guid? ActivePositionId = null,
    Guid? ResourceScopeUnitId = null,
    DateTimeOffset? When = null) : IRequest<AdminAccessDecisionDto>, IRequireUserAdmin;

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
        var decision = await _evaluator.EvaluateAsync(
            new AccessRequest(
                request.UserId,
                request.ActivePositionId,
                request.PermissionCode,
                request.ResourceScopeUnitId,
                request.When ?? DateTimeOffset.UtcNow),
            cancellationToken);
        return new AdminAccessDecisionDto(decision.Allowed, decision.Reason, decision.TraceId);
    }
}
