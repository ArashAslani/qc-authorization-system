using AccessManagement.Application.Abstractions;
using AccessManagement.Application.Authorization.Services;
using AccessManagement.Application.Common.Exceptions;
using AccessManagement.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

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
    private readonly IAccessEvaluator _evaluator;
    private readonly IApplicationDbContext _db;

    public GetAccessibleScopesQueryHandler(
        IActorAccessService actorAccess,
        IAccessEvaluator evaluator,
        IApplicationDbContext db)
    {
        _actorAccess = actorAccess;
        _evaluator = evaluator;
        _db = db;
    }

    public async Task<AccessibleScopesDto> Handle(GetAccessibleScopesQuery request, CancellationToken cancellationToken)
    {
        if (request.ActivePositionId is Guid positionId)
        {
            var now = DateTimeOffset.UtcNow;
            var assigned = _db.PositionAssignments
                .AsNoTracking()
                .Where(a => a.PositionId == positionId
                         && a.Personnel.IdentityUserId == request.SubjectUserId
                         && a.ValidFrom <= now
                         && (a.ValidTo == null || now <= a.ValidTo));
            if (request.ActorCompanyUnitId is Guid company)
            {
                assigned = assigned.Where(a => a.Position.CompanyUnitId == company);
            }

            if (!await assigned.AnyAsync(cancellationToken))
            {
                throw new ForbiddenAccessException("ActivePositionId is not assigned to the subject user.");
            }
        }

        var result = request.ActorCompanyUnitId is Guid companyId
            ? await _actorAccess.GetAccessibleRootsAsync(request.SubjectUserId, companyId, request.PermissionCode, cancellationToken)
            : await _evaluator.GetAccessibleScopesAsync(
                request.SubjectUserId, request.ActivePositionId, request.PermissionCode, cancellationToken);

        return new AccessibleScopesDto(result.IsUnrestricted, result.ScopeRootUnitIds, result.DeniedScopeUnitIds);
    }
}
