using AccessManagement.Application.Common.Exceptions;
using AccessManagement.Application.Common.Interfaces;
using AccessManagement.Domain.Authorization.ValueObjects;
using MediatR;
using Microsoft.EntityFrameworkCore;
using NotFoundException = AccessManagement.Application.Common.Exceptions.NotFoundException;

namespace AccessManagement.Application.Authorization.Queries.GetDelegationById;

public record GetDelegationByIdQuery(Guid Id) : IRequest<DelegationDetailsDto>;

public record DelegationDetailsDto(
    Guid Id,
    Guid DelegatorUserId,
    Guid DelegateUserId,
    Guid PermissionId,
    string PermissionCode,
    string PermissionResource,
    string PermissionAction,
    DateTimeOffset ValidFrom,
    DateTimeOffset? ValidTo,
    Guid? ScopeUnitId,
    bool Delegable,
    bool IsRevoked,
    bool IsActive);

public class GetDelegationByIdQueryHandler : IRequestHandler<GetDelegationByIdQuery, DelegationDetailsDto>
{
    private readonly IApplicationDbContext _context;

    public GetDelegationByIdQueryHandler(IApplicationDbContext context) => _context = context;

    public async Task<DelegationDetailsDto> Handle(GetDelegationByIdQuery request, CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var delegation = await _context.Delegations
            .AsNoTracking()
            .Include(d => d.Permission)
            .SingleOrDefaultAsync(d => d.Id == request.Id, cancellationToken);

        if (delegation is null)
        {
            throw new NotFoundException(nameof(Domain.Authorization.Delegation), request.Id);
        }

        return new DelegationDetailsDto(
            delegation.Id,
            delegation.DelegatorUserId,
            delegation.DelegateUserId,
            delegation.PermissionId,
            delegation.Permission.Code,
            delegation.Permission.Resource,
            delegation.Permission.Action,
            delegation.ValidFrom,
            delegation.ValidTo,
            delegation.ScopeUnitId,
            delegation.Delegable,
            delegation.IsRevoked,
            !delegation.IsRevoked && delegation.ValidFrom <= now && (delegation.ValidTo == null || delegation.ValidTo >= now));
    }
}
