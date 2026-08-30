using AccessManagement.Application.Authorization.Queries.EvaluateAccess;
using AccessManagement.Application.Common.Exceptions;
using AccessManagement.Application.Common.Security;
using MediatR;

namespace AccessManagement.Application.Authorization.Commands.EvaluateAccessBatch;

public sealed record EvaluateAccessBatchItem(
    Guid UserId,
    string PermissionCode,
    Guid? ActivePositionId = null,
    Guid? ResourceScopeUnitId = null);

public sealed record EvaluateAccessBatchRowResult(
    int Index,
    bool Succeeded,
    bool Allowed,
    string? Reason,
    string? Error);

public sealed record EvaluateAccessBatchResult(IReadOnlyList<EvaluateAccessBatchRowResult> Rows);

public sealed record EvaluateAccessBatchCommand(
    IReadOnlyList<EvaluateAccessBatchItem> Rows) : IRequest<EvaluateAccessBatchResult>, IRequireUserAdmin;

public sealed class EvaluateAccessBatchCommandHandler
    : IRequestHandler<EvaluateAccessBatchCommand, EvaluateAccessBatchResult>
{
    private readonly ISender _sender;

    public EvaluateAccessBatchCommandHandler(ISender sender) => _sender = sender;

    public async Task<EvaluateAccessBatchResult> Handle(
        EvaluateAccessBatchCommand request,
        CancellationToken cancellationToken)
    {
        var results = new List<EvaluateAccessBatchRowResult>(request.Rows.Count);
        for (var i = 0; i < request.Rows.Count; i++)
        {
            var row = request.Rows[i];
            try
            {
                var decision = await _sender.Send(
                    new EvaluateAccessQuery(row.UserId, row.PermissionCode, row.ActivePositionId, row.ResourceScopeUnitId),
                    cancellationToken);
                results.Add(new EvaluateAccessBatchRowResult(i, true, decision.Allowed, decision.Reason, null));
            }
            catch (Exception ex) when (ex is ForbiddenAccessException or InvalidOperationException)
            {
                results.Add(new EvaluateAccessBatchRowResult(i, false, false, null, ex.Message));
            }
        }

        return new EvaluateAccessBatchResult(results);
    }
}
