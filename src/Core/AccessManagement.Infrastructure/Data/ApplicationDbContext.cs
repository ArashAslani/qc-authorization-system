using System.Reflection;
using AccessManagement.Application.Common.Interfaces;
using AccessManagement.Domain.Authorization.Audit;
using AccessManagement.Domain.Authorization;
using AccessManagement.Domain.Organization;
using AccessManagement.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace AccessManagement.Infrastructure.Data;

public class ApplicationDbContext
    : IdentityDbContext<ApplicationUser, ApplicationRole, Guid>,
      IApplicationDbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<Personnel> Personnel => Set<Personnel>();
    public DbSet<Position> Positions => Set<Position>();
    public DbSet<PositionAssignment> PositionAssignments => Set<PositionAssignment>();
    public DbSet<OrganizationalUnit> OrganizationalUnits => Set<OrganizationalUnit>();

    public DbSet<ResourceCatalog> ResourceCatalogs => Set<ResourceCatalog>();
    public DbSet<ActionCatalog> ActionCatalogs => Set<ActionCatalog>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<Role> AuthorizationRoles => Set<Role>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<RoleGroup> RoleGroups => Set<RoleGroup>();
    public DbSet<RoleGroupMember> RoleGroupMembers => Set<RoleGroupMember>();
    public DbSet<Grant> Grants => Set<Grant>();
    public DbSet<Delegation> Delegations => Set<Delegation>();
    public DbSet<ModuleScopeConfig> ModuleScopeConfigs => Set<ModuleScopeConfig>();
    public DbSet<AuthorizationAuditEntry> AuthorizationAuditEntries => Set<AuthorizationAuditEntry>();
    public DbSet<AccessDecisionLog> AccessDecisionLogs => Set<AccessDecisionLog>();
    public DbSet<RevokedAccessToken> RevokedAccessTokens => Set<RevokedAccessToken>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

        foreach (var entityType in builder.Model.GetEntityTypes())
        {
            var idProperty = entityType.FindProperty("Id");
            if (idProperty?.ClrType == typeof(Guid))
            {
                builder.Entity(entityType.ClrType).Property("Id").ValueGeneratedOnAdd();
            }
        }
    }
}
