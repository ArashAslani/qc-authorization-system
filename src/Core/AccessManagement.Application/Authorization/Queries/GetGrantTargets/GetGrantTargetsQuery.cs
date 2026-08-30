using AccessManagement.Application.Authorization.Commands.GrantAccess;
using AccessManagement.Application.Authorization.Services;
using AccessManagement.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AccessManagement.Application.Authorization.Queries.GetGrantTargets;

public sealed record GetGrantTargetsQuery(Guid ActorUserId, Guid ActorCompanyUnitId) : IRequest<GrantTargetsDto>;

public sealed record GrantTargetPositionDto(Guid PositionId, string Code, string Title);

public sealed record GrantTargetUserDto(Guid UserId, Guid PersonnelId, string FullName);

public sealed record GrantTargetsDto(
    IReadOnlyList<GrantTargetPositionDto> Positions,
    IReadOnlyList<GrantTargetUserDto> Users);

public sealed class GetGrantTargetsQueryHandler : IRequestHandler<GetGrantTargetsQuery, GrantTargetsDto>
{
    private readonly IApplicationDbContext _db;
    private readonly IActorAccessService _actorAccess;
    private readonly LineManagerTargetPolicy _targets;

    public GetGrantTargetsQueryHandler(
        IApplicationDbContext db,
        IActorAccessService actorAccess,
        LineManagerTargetPolicy targets)
    {
        _db = db;
        _actorAccess = actorAccess;
        _targets = targets;
    }

    public async Task<GrantTargetsDto> Handle(GetGrantTargetsQuery request, CancellationToken cancellationToken)
    {
        var isAdmin = await _actorAccess.IsUserAdminAsync(request.ActorUserId, request.ActorCompanyUnitId, cancellationToken);
        List<Guid> positionIds;
        if (isAdmin)
        {
            positionIds = await _db.Positions
                .AsNoTracking()
                .Where(p => p.CompanyUnitId == request.ActorCompanyUnitId)
                .Select(p => p.Id)
                .ToListAsync(cancellationToken);
        }
        else
        {
            var actorPositions = await _targets.GetActorPositionIdsAsync(
                request.ActorUserId, request.ActorCompanyUnitId, cancellationToken);
            var subordinates = await _targets.GetSubordinatePositionIdsAsync(actorPositions, cancellationToken);
            positionIds = subordinates.ToList();
        }

        var positions = await _db.Positions
            .AsNoTracking()
            .Where(p => positionIds.Contains(p.Id))
            .Select(p => new GrantTargetPositionDto(p.Id, p.Code, p.Title))
            .ToListAsync(cancellationToken);

        var assigned = await _db.PositionAssignments
            .AsNoTracking()
            .Include(a => a.Personnel)
            .Where(a => a.ValidTo == null
                     && a.Personnel.IdentityUserId != null
                     && positionIds.Contains(a.PositionId))
            .Select(a => new
            {
                UserId = a.Personnel.IdentityUserId!.Value,
                a.PersonnelId,
                a.Personnel.FirstName,
                a.Personnel.LastName,
            })
            .ToListAsync(cancellationToken);

        var users = assigned
            .DistinctBy(a => a.UserId)
            .Select(a => new GrantTargetUserDto(a.UserId, a.PersonnelId, $"{a.FirstName} {a.LastName}".Trim()))
            .ToList();

        return new GrantTargetsDto(positions, users);
    }
}
