using qc_authorization.Application.Authorization.Evaluation;
using qc_authorization.Application.Common.Interfaces;
using qc_authorization.Application.QcBusinessIntegration.Models;
using qc_authorization.Application.QcBusinessIntegration.Providers;
using qc_authorization.Domain.Authorization.Enums;
using qc_authorization.Domain.Authorization.Evaluation;
using MediatR;

namespace qc_authorization.Application.QcBusinessIntegration.Commands.UpdateBom;

public record UpdateBomCommand(int BomId, string NewDescription, string NewRevision, Guid UserId) : IRequest<bool>;

public class UpdateBomCommandHandler : IRequestHandler<UpdateBomCommand, bool>
{
    private readonly IBomStore _store;
    private readonly BomAuthorizationContextProvider _contextProvider;
    private readonly IAccessEvaluator _accessEvaluator;

    public UpdateBomCommandHandler(
        IBomStore store,
        BomAuthorizationContextProvider contextProvider,
        IAccessEvaluator accessEvaluator)
    {
        _store = store;
        _contextProvider = contextProvider;
        _accessEvaluator = accessEvaluator;
    }

    public async Task<bool> Handle(UpdateBomCommand request, CancellationToken cancellationToken)
    {
        var bom = await _store.FindByIdAsync(request.BomId, cancellationToken)
            ?? throw new InvalidOperationException($"BOM {request.BomId} not found.");

        var authContext = await _contextProvider.GetContextAsync(request.BomId, cancellationToken);

        var accessRequest = AccessRequest.ForUser(
            request.UserId,
            action: "UPDATE",
            resource: "BOM",
            resourceId: bom.Id.ToString(),
            when: DateTimeOffset.UtcNow,
            context: authContext.ToContextDictionary());

        var decision = await _accessEvaluator.EvaluateAsync(accessRequest, cancellationToken);
        if (decision.Effect != Effect.Allow)
        {
            throw new UnauthorizedAccessException($"Access Denied for BOM.UPDATE: {decision.Reason}");
        }

        bom.Update(request.NewDescription, request.NewRevision);
        await _store.SaveAsync(bom, cancellationToken);
        return true;
    }
}
