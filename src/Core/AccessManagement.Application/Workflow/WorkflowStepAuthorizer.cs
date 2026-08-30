using AccessManagement.Application.Abstractions;
using AccessManagement.Domain.Authorization.Evaluation;

namespace AccessManagement.Application.Workflow;

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
        Guid userId,
        WorkflowStepRequirement requirement,
        DateTimeOffset when,
        CancellationToken cancellationToken = default)
    {
        Guid? scopeUnitId = Guid.TryParse(requirement.ResourceId, out var id) ? id : null;
        var request = AccessRequest.ForUser(
            userId,
            requirement.PermissionCode,
            resourceScopeUnitId: scopeUnitId,
            when: when);

        return _evaluator.EvaluateAsync(request, cancellationToken);
    }
}
