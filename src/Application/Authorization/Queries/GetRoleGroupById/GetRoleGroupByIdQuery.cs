using qc_authorization.Application.Common.Exceptions;
using qc_authorization.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using NotFoundException = qc_authorization.Application.Common.Exceptions.NotFoundException;

namespace qc_authorization.Application.Authorization.Queries.GetRoleGroupById;

public record GetRoleGroupByIdQuery(int Id) : IRequest<RoleGroupDetailsDto>;

public record RoleGroupDetailsDto(
    int Id,
    string Code,
    string Name,
    string? Description,
    IReadOnlyList<RoleGroupMemberItemDto> MemberRoles);

public record RoleGroupMemberItemDto(int RoleId, string Code, string Name, string? Description);

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
            memberRoles);
    }
}
