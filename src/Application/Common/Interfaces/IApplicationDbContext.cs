using qc_authorization.Domain.Authorization;
using qc_authorization.Domain.Organization;

namespace qc_authorization.Application.Common.Interfaces;

public interface IApplicationDbContext
{
    DbSet<Personnel> Personnel { get; }
    DbSet<Position> Positions { get; }
    DbSet<PositionAssignment> PositionAssignments { get; }

    DbSet<Permission> Permissions { get; }
    DbSet<Role> Roles { get; }
    DbSet<RolePermission> RolePermissions { get; }
    DbSet<Grant> Grants { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
