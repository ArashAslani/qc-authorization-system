using AccessManagement.Application.Common.Exceptions;
using AccessManagement.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using NotFoundException = AccessManagement.Application.Common.Exceptions.NotFoundException;

namespace AccessManagement.Application.Authorization.Queries.GetRoleGroupById;

public record GetRoleGroupByIdQuery(Guid Id) : IRequest<RoleGroupDetailsDto>;

public record RoleGroupDetailsDto(
    Guid Id,
    string Code,
    string Name,
    string? Description,
    Domain.Authorization.Enums.CatalogStatus Status,
    IReadOnlyList<RoleGroupMemberItemDto> MemberRoles);

public record RoleGroupMemberItemDto(Guid RoleId, string Code, string Name, string? Description);

public class GetRoleGroupByIdQueryHandler : IRequestHandler<GetRoleGroupByIdQuery, RoleGroupDetailsDto>
{
    private readonly IApplicationDbContext _context;

    public GetRoleGroupByIdQueryHandler(IApplicationDbContext context) => _context = context;

    public async Task<RoleGroupDetailsDto> Handle(GetRoleGroupByIdQuery request, CancellationToken cancellationToken)
    {
        var group = await _context.RoleGroups
            .AsNoTracking()
            .SingleOrDefaultAsync(rg => rg.Id == request.Id, cancellationToken);

        if (group is null)
        {
            throw new NotFoundException(nameof(Domain.Authorization.RoleGroup), request.Id);
        }

        var memberRoles = await _context.RoleGroupMembers
            .AsNoTracking()
            .Include(m => m.Role)
            .Where(m => m.RoleGroupId == request.Id)
            .OrderBy(m => m.Role.Code)
            .Select(m => new RoleGroupMemberItemDto(
                m.RoleId,
                m.Role.Code,
                m.Role.Name,
                m.Role.Description))
            .ToListAsync(cancellationToken);

        return new RoleGroupDetailsDto(
            group.Id,
            group.Code,
            group.Name,
            group.Description,
            group.Status,
            memberRoles);
    }
}
