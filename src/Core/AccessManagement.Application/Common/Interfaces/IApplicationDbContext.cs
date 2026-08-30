using AccessManagement.Domain.Authorization;
using AccessManagement.Domain.Authorization.Audit;
using AccessManagement.Domain.Organization;
using Microsoft.EntityFrameworkCore;

namespace AccessManagement.Application.Common.Interfaces;

public interface IApplicationDbContext
{
    DbSet<Personnel> Personnel { get; }
    DbSet<Position> Positions { get; }
    DbSet<PositionAssignment> PositionAssignments { get; }
    DbSet<OrganizationalUnit> OrganizationalUnits { get; }

    DbSet<ResourceCatalog> ResourceCatalogs { get; }
    DbSet<ActionCatalog> ActionCatalogs { get; }
    DbSet<Permission> Permissions { get; }
    DbSet<Role> AuthorizationRoles { get; }
    DbSet<RolePermission> RolePermissions { get; }
    DbSet<RoleGroup> RoleGroups { get; }
    DbSet<RoleGroupMember> RoleGroupMembers { get; }
    DbSet<Grant> Grants { get; }
    DbSet<Delegation> Delegations { get; }
    DbSet<ModuleScopeConfig> ModuleScopeConfigs { get; }
    DbSet<AuthorizationAuditEntry> AuthorizationAuditEntries { get; }
    DbSet<AccessDecisionLog> AccessDecisionLogs { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
