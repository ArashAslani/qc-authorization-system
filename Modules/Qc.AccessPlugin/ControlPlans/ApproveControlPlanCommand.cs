using MediatR;

namespace Qc.AccessPlugin.ControlPlans;

public sealed record ApproveControlPlanCommand(
    Guid ControlPlanId,
    Guid UserId,
    Guid? ActivePositionId) : IRequest<bool>;

public sealed class ApproveControlPlanCommandHandler : IRequestHandler<ApproveControlPlanCommand, bool>
{
    private readonly IControlPlanStore _store;
    private readonly ControlPlanApprovalGuard _guard;

    public ApproveControlPlanCommandHandler(IControlPlanStore store, ControlPlanApprovalGuard guard)
    {
        _store = store;
        _guard = guard;
    }

    public async Task<bool> Handle(ApproveControlPlanCommand request, CancellationToken cancellationToken)
    {
        var plan = await _store.FindByIdAsync(request.ControlPlanId, cancellationToken)
            ?? throw new InvalidOperationException($"Control plan {request.ControlPlanId} not found.");

        await _guard.EnsureCanApproveAsync(plan, request.UserId, request.ActivePositionId, cancellationToken);
        plan.Approve();
        await _store.SaveAsync(plan, cancellationToken);
        return true;
    }
}
