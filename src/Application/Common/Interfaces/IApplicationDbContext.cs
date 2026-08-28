using qc_authorization.Domain.Authorization;
using qc_authorization.Domain.Authorization.Audit;
using qc_authorization.Domain.Organization;
using Microsoft.EntityFrameworkCore;

namespace qc_authorization.Application.Common.Interfaces;

public interface IApplicationDbContext
{
    DbSet<Personnel> Personnel { get; }
    DbSet<Position> Positions { get; }
    DbSet<PositionAssignment> PositionAssignments { get; }

    DbSet<ResourceCatalog> ResourceCatalogs { get; }
    DbSet<ActionCatalog> ActionCatalogs { get; }
    DbSet<Permission> Permissions { get; }
    DbSet<Role> AuthorizationRoles { get; }
    DbSet<RolePermission> RolePermissions { get; }
    DbSet<RoleGroup> RoleGroups { get; }
    DbSet<RoleGroupMember> RoleGroupMembers { get; }
    DbSet<Grant> Grants { get; }
    DbSet<Delegation> Delegations { get; }
    DbSet<AuthorizationAuditEntry> AuthorizationAuditEntries { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
