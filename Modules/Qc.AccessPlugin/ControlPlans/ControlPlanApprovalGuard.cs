using AccessManagement.Application.Abstractions;
using AccessManagement.Domain.Authorization.Evaluation;

namespace Qc.AccessPlugin.ControlPlans;

/// <summary>
/// Product guard: business status is checked here, never inside the Core engine.
/// Engine answers Allow/Deny for CONTROLPLAN.APPROVE; Draft/UnderReview is QC's rule.
/// </summary>
public sealed class ControlPlanApprovalGuard
{
    private readonly IAccessEvaluator _evaluator;

    public ControlPlanApprovalGuard(IAccessEvaluator evaluator) => _evaluator = evaluator;

    public async Task EnsureCanApproveAsync(
        ControlPlan plan,
        Guid userId,
        Guid? activePositionId,
        CancellationToken ct = default)
    {
        if (plan.Status == ControlPlanStatus.Draft)
        {
            throw new InvalidOperationException(
                "Control plan is Draft. Submit for review before approval (QC business rule, not Engine).");
        }

        var decision = await _evaluator.EvaluateAsync(
            new AccessRequest(
                userId,
                activePositionId,
                QcPermissions.ControlPlanApprove,
                plan.ScopeUnitId,
                DateTimeOffset.UtcNow),
            ct);

        if (!decision.Allowed)
        {
            throw new UnauthorizedAccessException(
                $"CONTROLPLAN.APPROVE denied: {decision.Reason}");
        }
    }
}
