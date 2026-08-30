using AccessManagement.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AccessManagement.Application.Authorization.Queries.GetPermissions;

public record GetPermissionsQuery(
    string? Resource = null,
    string? Action = null,
    string? SearchTerm = null) : IRequest<IReadOnlyList<PermissionDto>>;

public record PermissionDto(
    Guid Id,
    string Code,
    string Resource,
    string Action,
    string? Description,
    Guid? ResourceCatalogId,
    Guid? ActionCatalogId);

public class GetPermissionsQueryHandler : IRequestHandler<GetPermissionsQuery, IReadOnlyList<PermissionDto>>
{
    private readonly IApplicationDbContext _context;

    public GetPermissionsQueryHandler(IApplicationDbContext context) => _context = context;

    public async Task<IReadOnlyList<PermissionDto>> Handle(GetPermissionsQuery request, CancellationToken cancellationToken)
    {
        var query = _context.Permissions.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Resource))
        {
            query = query.Where(p => p.Resource == request.Resource.Trim().ToUpper());
        }

        if (!string.IsNullOrWhiteSpace(request.Action))
        {
            query = query.Where(p => p.Action == request.Action.Trim().ToUpper());
        }

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var term = request.SearchTerm.Trim().ToLower();
            query = query.Where(p => p.Code.ToLower().Contains(term) || (p.Description != null && p.Description.ToLower().Contains(term)));
        }

        return await query
            .OrderBy(p => p.Code)
            .Select(p => new PermissionDto(
                p.Id,
                p.Code,
                p.Resource,
                p.Action,
                p.Description,
                p.ResourceCatalogId,
                p.ActionCatalogId))
            .ToListAsync(cancellationToken);
    }
}
