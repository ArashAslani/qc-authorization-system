using qc_authorization.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace qc_authorization.Application.Authorization.Queries.GetRoles;

public record GetRolesQuery(string? SearchTerm = null) : IRequest<IReadOnlyList<RoleDto>>;

public record RoleDto(Guid Id, string Code, string Name, string? Description, int PermissionCount);

public class GetRolesQueryHandler : IRequestHandler<GetRolesQuery, IReadOnlyList<RoleDto>>
{
    private readonly IApplicationDbContext _context;

    public GetRolesQueryHandler(IApplicationDbContext context) => _context = context;

    public async Task<IReadOnlyList<RoleDto>> Handle(GetRolesQuery request, CancellationToken cancellationToken)
    {
        var query = _context.AuthorizationRoles.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var term = request.SearchTerm.Trim().ToLower();
            query = query.Where(r => r.Code.ToLower().Contains(term) || r.Name.ToLower().Contains(term));
        }

        return await query
            .OrderBy(r => r.Code)
            .Select(r => new RoleDto(
                r.Id,
                r.Code,
                r.Name,
                r.Description,
                _context.RolePermissions.Count(rp => rp.RoleId == r.Id)))
            .ToListAsync(cancellationToken);
    }
}
