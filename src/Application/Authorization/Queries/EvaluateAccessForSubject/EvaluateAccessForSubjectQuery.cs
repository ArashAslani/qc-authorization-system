using qc_authorization.Application.Authorization.Evaluation;
using qc_authorization.Domain.Authorization.Enums;
using qc_authorization.Domain.Authorization.Evaluation;
using MediatR;

namespace qc_authorization.Application.Authorization.Queries.EvaluateAccessForSubject;

public record EvaluateAccessForSubjectQuery(
    SubjectType SubjectType,
    Guid SubjectId,
    Guid? UserId,
    string Action,
    string Resource,
    string? ResourceId,
    DateTimeOffset When) : IRequest<AdminAccessDecisionDto>;

public record AdminAccessDecisionDto(
    string Effect,
    string Reason,
    AdminDecisionTraceDto Trace);

public record AdminDecisionTraceDto(
    string TraceId,
    string SubjectType,
    Guid SubjectId,
    string RequestedPermission,
    string Resource,
    string? ResourceId,
    string FinalDecision,
    string Reason,
    IReadOnlyList<AdminTraceGrantSummaryDto> CandidateGrants,
    IReadOnlyList<AdminTraceGrantSummaryDto> ApplicableGrants,
    IReadOnlyList<AdminTraceRejectedGrantDto> RejectedGrants,
    IReadOnlyList<AdminTraceConflictEntryDto> ConflictResolution);

public record AdminTraceGrantSummaryDto(
    Guid Id,
    string SubjectType,
    Guid SubjectId,
    Guid? SubjectUserId,
    string SourceType,
    Guid SourceId,
    int Priority,
    string Effect);

public record AdminTraceRejectedGrantDto(
    Guid GrantId,
    string SourceType,
    Guid SourceId,
    string Reason);

public record AdminTraceConflictEntryDto(
    Guid GrantId,
    string SourceType,
    Guid SourceId,
    int Priority,
    string Effect,
    bool Won);

public class EvaluateAccessForSubjectQueryHandler : IRequestHandler<EvaluateAccessForSubjectQuery, AdminAccessDecisionDto>
{
    private readonly IAccessEvaluator _evaluator;

    public EvaluateAccessForSubjectQueryHandler(IAccessEvaluator evaluator) => _evaluator = evaluator;

    public async Task<AdminAccessDecisionDto> Handle(EvaluateAccessForSubjectQuery request, CancellationToken cancellationToken)
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
        var trace = decision.Trace;

        var candidates = trace.CandidateGrants
            .Select(g => new AdminTraceGrantSummaryDto(
                g.Id,
                g.SubjectType.ToString(),
                g.SubjectId,
                g.SubjectUserId,
                g.SourceType.ToString(),
                g.SourceId,
                g.Priority,
                g.Effect.ToString()))
            .ToList();

        var applicable = trace.ApplicableGrants
            .Select(g => new AdminTraceGrantSummaryDto(
                g.Id,
                g.SubjectType.ToString(),
                g.SubjectId,
                g.SubjectUserId,
                g.SourceType.ToString(),
                g.SourceId,
                g.Priority,
                g.Effect.ToString()))
            .ToList();

        var rejected = trace.RejectedGrants
            .Select(r => new AdminTraceRejectedGrantDto(
                r.GrantId,
                r.SourceType.ToString(),
                r.SourceId,
                r.Reason))
            .ToList();

        var conflicts = trace.ConflictResolution
            .Select(c => new AdminTraceConflictEntryDto(
                c.GrantId,
                c.SourceType.ToString(),
                c.SourceId,
                c.Priority,
                c.Effect.ToString(),
                c.Won))
            .ToList();

        var traceDto = new AdminDecisionTraceDto(
            trace.TraceId,
            trace.Subject.ToString(),
            trace.SubjectId,
            trace.RequestedPermission,
            trace.Resource,
            trace.ResourceId,
            trace.FinalDecision.ToString(),
            trace.Reason,
            candidates,
            applicable,
            rejected,
            conflicts);

        return new AdminAccessDecisionDto(
            decision.Effect.ToString(),
            decision.Reason.ToString(),
            traceDto);
    }
}
