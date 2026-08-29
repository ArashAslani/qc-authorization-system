using qc_authorization.Application.Authorization.Evaluation;
using qc_authorization.Application.Common.Interfaces;
using qc_authorization.Application.QcBusinessIntegration.Models;
using qc_authorization.Application.QcBusinessIntegration.Providers;
using qc_authorization.Domain.Authorization.Enums;
using qc_authorization.Domain.Authorization.Evaluation;
using MediatR;

namespace qc_authorization.Application.QcBusinessIntegration.Commands.UpdateControlPlan;

public record UpdateControlPlanCommand(Guid ControlPlanId, string NewTitle, Guid UserId) : IRequest<bool>;

public class UpdateControlPlanCommandHandler : IRequestHandler<UpdateControlPlanCommand, bool>
{
    private readonly IControlPlanStore _store;
    private readonly ControlPlanAuthorizationContextProvider _contextProvider;
    private readonly IAccessEvaluator _accessEvaluator;

    public UpdateControlPlanCommandHandler(
        IControlPlanStore store,
        ControlPlanAuthorizationContextProvider contextProvider,
        IAccessEvaluator accessEvaluator)
    {
        _store = store;
        _contextProvider = contextProvider;
        _accessEvaluator = accessEvaluator;
    }

    public async Task<bool> Handle(UpdateControlPlanCommand request, CancellationToken cancellationToken)
    {
        var controlPlan = await _store.FindByIdAsync(request.ControlPlanId, cancellationToken)
            ?? throw new InvalidOperationException($"Control plan {request.ControlPlanId} not found.");

        var authContext = await _contextProvider.GetContextAsync(request.ControlPlanId, cancellationToken);

        var accessRequest = AccessRequest.ForUser(
            request.UserId,
            action: "UPDATE",
            resource: "CONTROL_PLAN",
            resourceId: controlPlan.Id.ToString(),
            when: DateTimeOffset.UtcNow,
            context: authContext.ToContextDictionary());

        var decision = await _accessEvaluator.EvaluateAsync(accessRequest, cancellationToken);
        if (decision.Effect != Effect.Allow)
        {
            throw new UnauthorizedAccessException($"Access Denied for CONTROL_PLAN.UPDATE: {decision.Reason}");
        }

        controlPlan.UpdateTitle(request.NewTitle);
        await _store.SaveAsync(controlPlan, cancellationToken);
        return true;
    }
}
