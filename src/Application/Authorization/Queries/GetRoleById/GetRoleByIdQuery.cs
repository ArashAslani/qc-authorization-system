using qc_authorization.Application.Common.Exceptions;
using qc_authorization.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using NotFoundException = qc_authorization.Application.Common.Exceptions.NotFoundException;

namespace qc_authorization.Application.Authorization.Queries.GetRoleById;

public record GetRoleByIdQuery(Guid Id) : IRequest<RoleDetailsDto>;

public record RoleDetailsDto(
    Guid Id,
    string Code,
    string Name,
    string? Description,
    IReadOnlyList<RolePermissionItemDto> Permissions,
    IReadOnlyList<RoleGroupItemDto> Groups);

public record RolePermissionItemDto(Guid PermissionId, string Code, string Resource, string Action, string? Description);

public record RoleGroupItemDto(Guid RoleGroupId, string Code, string Name);

public class GetRoleByIdQueryHandler : IRequestHandler<GetRoleByIdQuery, RoleDetailsDto>
{
    private readonly IApplicationDbContext _context;

    public GetRoleByIdQueryHandler(IApplicationDbContext context) => _context = context;

    public async Task<RoleDetailsDto> Handle(GetRoleByIdQuery request, CancellationToken cancellationToken)
    {
        var role = await _context.AuthorizationRoles
            .AsNoTracking()
            .SingleOrDefaultAsync(r => r.Id == request.Id, cancellationToken);

        if (role is null)
        {
            throw new NotFoundException(nameof(Domain.Authorization.Role), request.Id);
        }

        var permissions = await _context.RolePermissions
            .AsNoTracking()
            .Include(rp => rp.Permission)
            .Where(rp => rp.RoleId == request.Id)
            .OrderBy(rp => rp.Permission.Code)
            .Select(rp => new RolePermissionItemDto(
                rp.PermissionId,
                rp.Permission.Code,
                rp.Permission.Resource,
                rp.Permission.Action,
                rp.Permission.Description))
            .ToListAsync(cancellationToken);

        var groups = await _context.RoleGroupMembers
            .AsNoTracking()
            .Include(rgm => rgm.RoleGroup)
            .Where(rgm => rgm.RoleId == request.Id)
            .OrderBy(rgm => rgm.RoleGroup.Code)
            .Select(rgm => new RoleGroupItemDto(
                rgm.RoleGroupId,
                rgm.RoleGroup.Code,
                rgm.RoleGroup.Name))
            .ToListAsync(cancellationToken);

        return new RoleDetailsDto(
            role.Id,
            role.Code,
            role.Name,
            role.Description,
            permissions,
            groups);
    }
}
