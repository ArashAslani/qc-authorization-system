using qc_authorization.Application.Common.Interfaces;
using qc_authorization.Domain.Authorization.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace qc_authorization.Application.Authorization.Queries.GetRoleGroups;

public record GetRoleGroupsQuery(string? SearchTerm = null, string? MemberRoleCode = null) : IRequest<IReadOnlyList<RoleGroupDto>>;

public record RoleGroupDto(
    Guid Id,
    string Code,
    string Name,
    string? Description,
    CatalogStatus Status,
    int MemberRoleCount);

public class GetRoleGroupsQueryHandler : IRequestHandler<GetRoleGroupsQuery, IReadOnlyList<RoleGroupDto>>
{
    private readonly IApplicationDbContext _context;

    public GetRoleGroupsQueryHandler(IApplicationDbContext context) => _context = context;

    public async Task<IReadOnlyList<RoleGroupDto>> Handle(GetRoleGroupsQuery request, CancellationToken cancellationToken)
    {
        var query = _context.RoleGroups.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var term = request.SearchTerm.Trim().ToLower();
            query = query.Where(rg => rg.Code.ToLower().Contains(term) || rg.Name.ToLower().Contains(term));
        }

        if (!string.IsNullOrWhiteSpace(request.MemberRoleCode))
        {
            var roleCode = request.MemberRoleCode.Trim().ToUpperInvariant();
            query = query.Where(rg => _context.RoleGroupMembers
                .Any(m => m.RoleGroupId == rg.Id && m.Role.Code == roleCode));
        }

        return await query
            .OrderBy(rg => rg.Code)
            .Select(rg => new RoleGroupDto(
                rg.Id,
                rg.Code,
                rg.Name,
                rg.Description,
                rg.Status,
                _context.RoleGroupMembers.Count(m => m.RoleGroupId == rg.Id)))
            .ToListAsync(cancellationToken);
    }
}
