using qc_authorization.Application.Common.Exceptions;
using qc_authorization.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using NotFoundException = qc_authorization.Application.Common.Exceptions.NotFoundException;

namespace qc_authorization.Application.Authorization.Queries.GetPermissionById;

public record GetPermissionByIdQuery(Guid Id) : IRequest<PermissionDetailsDto>;

public record PermissionDetailsDto(
    Guid Id,
    string Code,
    string Resource,
    string Action,
    string? Description,
    Guid? ResourceCatalogId,
    string? ResourceCatalogName,
    Guid? ActionCatalogId,
    string? ActionCatalogName,
    int RolesCount,
    int GrantsCount);

public class GetPermissionByIdQueryHandler : IRequestHandler<GetPermissionByIdQuery, PermissionDetailsDto>
{
    private readonly IApplicationDbContext _context;

    public GetPermissionByIdQueryHandler(IApplicationDbContext context) => _context = context;

    public async Task<PermissionDetailsDto> Handle(GetPermissionByIdQuery request, CancellationToken cancellationToken)
    {
        var permission = await _context.Permissions
            .AsNoTracking()
            .Include(p => p.ResourceCatalog)
            .Include(p => p.ActionCatalog)
            .SingleOrDefaultAsync(p => p.Id == request.Id, cancellationToken);

        if (permission is null)
        {
            throw new NotFoundException(nameof(Domain.Authorization.Permission), request.Id);
        }

        var rolesCount = await _context.RolePermissions
            .AsNoTracking()
            .CountAsync(rp => rp.PermissionId == request.Id, cancellationToken);

        var grantsCount = await _context.Grants
            .AsNoTracking()
            .CountAsync(g => g.PermissionId == request.Id, cancellationToken);

        return new PermissionDetailsDto(
            permission.Id,
            permission.Code,
            permission.Resource,
            permission.Action,
            permission.Description,
            permission.ResourceCatalogId,
            permission.ResourceCatalog?.Name,
            permission.ActionCatalogId,
            permission.ActionCatalog?.Name,
            rolesCount,
            grantsCount);
    }
}
