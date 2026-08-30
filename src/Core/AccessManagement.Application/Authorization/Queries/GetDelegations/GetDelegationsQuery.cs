using AccessManagement.Application.Common.Interfaces;
using AccessManagement.Domain.Authorization.ValueObjects;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AccessManagement.Application.Authorization.Queries.GetDelegations;

public record GetDelegationsQuery(
    Guid? DelegatorUserId = null,
    Guid? DelegateUserId = null,
    Guid? PermissionId = null,
    bool? ActiveOnly = null) : IRequest<IReadOnlyList<DelegationDto>>;

public record DelegationDto(
    Guid Id,
    Guid DelegatorUserId,
    Guid DelegateUserId,
    Guid PermissionId,
    string PermissionCode,
    DateTimeOffset ValidFrom,
    DateTimeOffset? ValidTo,
    Guid? ScopeUnitId,
    bool Delegable,
    bool IsRevoked,
    bool IsActive);

public class GetDelegationsQueryHandler : IRequestHandler<GetDelegationsQuery, IReadOnlyList<DelegationDto>>
{
    private readonly IApplicationDbContext _context;

    public GetDelegationsQueryHandler(IApplicationDbContext context) => _context = context;

    public async Task<IReadOnlyList<DelegationDto>> Handle(GetDelegationsQuery request, CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var query = _context.Delegations
            .AsNoTracking()
            .Include(d => d.Permission)
            .AsQueryable();

        if (request.DelegatorUserId.HasValue)
        {
            query = query.Where(d => d.DelegatorUserId == request.DelegatorUserId.Value);
        }

        if (request.DelegateUserId.HasValue)
        {
            query = query.Where(d => d.DelegateUserId == request.DelegateUserId.Value);
        }

        if (request.PermissionId.HasValue)
        {
            query = query.Where(d => d.PermissionId == request.PermissionId.Value);
        }

        if (request.ActiveOnly == true)
        {
            query = query.Where(d => !d.IsRevoked && d.ValidFrom <= now && (d.ValidTo == null || d.ValidTo >= now));
        }

        return await query
            .OrderByDescending(d => d.Id)
            .Select(d => new DelegationDto(
                d.Id,
                d.DelegatorUserId,
                d.DelegateUserId,
                d.PermissionId,
                d.Permission.Code,
                d.ValidFrom,
                d.ValidTo,
                d.ScopeUnitId,
                d.Delegable,
                d.IsRevoked,
                !d.IsRevoked && d.ValidFrom <= now && (d.ValidTo == null || d.ValidTo >= now)))
            .ToListAsync(cancellationToken);
    }
}
