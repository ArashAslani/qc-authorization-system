using qc_authorization.Application.Authorization.Evaluation;
using qc_authorization.Application.Common.Interfaces;
using qc_authorization.Application.QcBusinessIntegration.Models;
using qc_authorization.Application.QcBusinessIntegration.Providers;
using qc_authorization.Domain.Authorization.Enums;
using qc_authorization.Domain.Authorization.Evaluation;
using MediatR;

namespace qc_authorization.Application.QcBusinessIntegration.Commands.ApproveControlPlan;

public record ApproveControlPlanCommand(int ControlPlanId, Guid UserId) : IRequest<bool>;

public class ApproveControlPlanCommandHandler : IRequestHandler<ApproveControlPlanCommand, bool>
{
    private readonly IControlPlanStore _store;
    private readonly ControlPlanAuthorizationContextProvider _contextProvider;
    private readonly IAccessEvaluator _accessEvaluator;

    public ApproveControlPlanCommandHandler(
        IControlPlanStore store,
        ControlPlanAuthorizationContextProvider contextProvider,
        IAccessEvaluator accessEvaluator)
    {
        _store = store;
        _contextProvider = contextProvider;
        _accessEvaluator = accessEvaluator;
    }

    public async Task<bool> Handle(ApproveControlPlanCommand request, CancellationToken cancellationToken)
    {
        // 1. Load Business Entity
        var controlPlan = await _store.FindByIdAsync(request.ControlPlanId, cancellationToken)
            ?? throw new InvalidOperationException($"Control plan {request.ControlPlanId} not found.");

        // 2. Build Resource Authorization Context
        var authContext = await _contextProvider.GetContextAsync(request.ControlPlanId, cancellationToken);

        // 3. Evaluate Authorization via Core AccessEvaluationEngine
        var accessRequest = AccessRequest.ForUser(
            request.UserId,
            action: "APPROVE",
            resource: "CONTROL_PLAN",
            resourceId: controlPlan.Id.ToString(),
            when: DateTimeOffset.UtcNow,
            context: authContext.ToContextDictionary());

        var decision = await _accessEvaluator.EvaluateAsync(accessRequest, cancellationToken);
        if (decision.Effect != Effect.Allow)
        {
            throw new UnauthorizedAccessException($"Access Denied for CONTROL_PLAN.APPROVE: {decision.Reason}");
        }

        // 4. Apply Business Invariants & Rules
        controlPlan.Approve();

        // 5. Save Business Changes
        await _store.SaveAsync(controlPlan, cancellationToken);
        return true;
    }
}
