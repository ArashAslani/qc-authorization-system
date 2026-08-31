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
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;

    public GetAccessibleScopesQueryHandler(
        IActorAccessService actorAccess,
        IApplicationDbContext db,
        ICurrentUser currentUser)
    {
        _actorAccess = actorAccess;
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<AccessibleScopesDto> Handle(GetAccessibleScopesQuery request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is not Guid actorUserId)
        {
            throw new UnauthorizedAccessException();
        }

        if (request.SubjectUserId != actorUserId
            && !await _actorAccess.IsUserAdminAsync(actorUserId, request.ActorCompanyUnitId ?? _currentUser.ActiveCompanyId, cancellationToken))
        {
            throw new ForbiddenAccessException("SubjectUserId must match the authenticated user.");
        }

        if (request.ActorCompanyUnitId is null && _currentUser.ActiveCompanyId is null)
        {
            throw new ForbiddenAccessException("An active company workspace is required.");
        }

        var companyId = request.ActorCompanyUnitId ?? _currentUser.ActiveCompanyId!.Value;

        if (request.ActivePositionId is Guid positionId)
        {
            var now = DateTimeOffset.UtcNow;
            var assigned = _db.PositionAssignments
                .AsNoTracking()
                .Where(a => a.PositionId == positionId
                         && a.Personnel.IdentityUserId == request.SubjectUserId
                         && a.Position.CompanyUnitId == companyId
                         && a.ValidFrom <= now
                         && (a.ValidTo == null || now <= a.ValidTo));

            if (!await assigned.AnyAsync(cancellationToken))
            {
                throw new ForbiddenAccessException("ActivePositionId is not assigned to the subject user.");
            }
        }

        var result = await _actorAccess.GetAccessibleRootsAsync(
            request.SubjectUserId, companyId, request.PermissionCode, cancellationToken);

        return new AccessibleScopesDto(result.IsUnrestricted, result.ScopeRootUnitIds, result.DeniedScopeUnitIds);
    }
}
