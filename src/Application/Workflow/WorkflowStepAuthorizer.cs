using qc_authorization.Application.Authorization.Evaluation;
using qc_authorization.Domain.Authorization.Evaluation;

namespace qc_authorization.Application.Workflow;

public sealed record WorkflowStepRequirement(
    string PermissionCode,
    string Resource,
    string? ResourceId = null);

public sealed class WorkflowStepAuthorizer
{
    private readonly IAccessEvaluator _evaluator;

    public WorkflowStepAuthorizer(IAccessEvaluator evaluator)
    {
        _evaluator = evaluator;
    }

    public Task<AccessDecision> AuthorizeAsync(
        int systemUserId,
        WorkflowStepRequirement requirement,
        DateTimeOffset when,
        CancellationToken cancellationToken = default)
    {
        var parts = requirement.PermissionCode.Split('.', 2, StringSplitOptions.TrimEntries);
        var action = parts.Length == 2 ? parts[1] : requirement.PermissionCode;
        var resource = parts.Length == 2 ? parts[0] : requirement.Resource;

        var context = new Dictionary<string, object>
        {
            ["WorkflowStep"] = requirement.PermissionCode,
        };

        var request = new AccessRequest(
            Domain.Authorization.Enums.SubjectType.User,
            systemUserId,
            action,
            resource,
            requirement.ResourceId,
            when,
            context);

        return _evaluator.EvaluateAsync(request, cancellationToken);
    }
}
