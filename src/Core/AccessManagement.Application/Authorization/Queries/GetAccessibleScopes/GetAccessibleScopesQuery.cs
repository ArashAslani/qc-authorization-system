using AccessManagement.Application.Authorization.Services;
using MediatR;

namespace AccessManagement.Application.Authorization.Queries.GetAccessibleScopes;

public sealed record GetAccessibleScopesQuery(
    Guid SubjectUserId,
    Guid? ActivePositionId,
    string PermissionCode,
    Guid? ActorCompanyUnitId = null) : IRequest<AccessibleScopesDto>;

public sealed record AccessibleScopesDto(
    bool IsUnrestricted,
    IReadOnlyList<Guid> ScopeRootUnitIds,
    IReadOnlyList<Guid> DeniedScopeUnitIds);

public sealed class GetAccessibleScopesQueryHandler : IRequestHandler<GetAccessibleScopesQuery, AccessibleScopesDto>
{
    private readonly IActorAccessService _actorAccess;
    private readonly AccessManagement.Application.Abstractions.IAccessEvaluator _evaluator;

    public GetAccessibleScopesQueryHandler(
        IActorAccessService actorAccess,
        AccessManagement.Application.Abstractions.IAccessEvaluator evaluator)
    {
        _actorAccess = actorAccess;
        _evaluator = evaluator;
    }

    public async Task<AccessibleScopesDto> Handle(GetAccessibleScopesQuery request, CancellationToken cancellationToken)
    {
        var result = request.ActorCompanyUnitId is Guid company
            ? await _actorAccess.GetAccessibleRootsAsync(request.SubjectUserId, company, request.PermissionCode, cancellationToken)
            : await _evaluator.GetAccessibleScopesAsync(
                request.SubjectUserId, request.ActivePositionId, request.PermissionCode, cancellationToken);

        return new AccessibleScopesDto(result.IsUnrestricted, result.ScopeRootUnitIds, result.DeniedScopeUnitIds);
    }
}
