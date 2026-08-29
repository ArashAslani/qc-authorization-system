using qc_authorization.Application.Common.Exceptions;
using qc_authorization.Application.Common.Interfaces;
using qc_authorization.Domain.Authorization.ValueObjects;
using MediatR;
using Microsoft.EntityFrameworkCore;
using NotFoundException = qc_authorization.Application.Common.Exceptions.NotFoundException;

namespace qc_authorization.Application.Authorization.Queries.GetDelegationById;

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
    ScopeKind ScopeKind,
    string? ScopeIdentifier,
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
            delegation.ScopeKind,
            delegation.ScopeIdentifier,
            delegation.Delegable,
            delegation.IsRevoked,
            !delegation.IsRevoked && delegation.ValidFrom <= now && (delegation.ValidTo == null || delegation.ValidTo >= now));
    }
}
