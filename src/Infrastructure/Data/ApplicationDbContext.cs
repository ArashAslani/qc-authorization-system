using System.Reflection;
using qc_authorization.Application.Common.Interfaces;
using qc_authorization.Domain.Authorization.Audit;
using qc_authorization.Domain.Authorization;
using qc_authorization.Domain.Organization;
using qc_authorization.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace qc_authorization.Infrastructure.Data;

public class ApplicationDbContext
    : IdentityDbContext<ApplicationUser, ApplicationRole, Guid>,
      IApplicationDbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<Personnel> Personnel => Set<Personnel>();
    public DbSet<Position> Positions => Set<Position>();
    public DbSet<PositionAssignment> PositionAssignments => Set<PositionAssignment>();

    public DbSet<ResourceCatalog> ResourceCatalogs => Set<ResourceCatalog>();
    public DbSet<ActionCatalog> ActionCatalogs => Set<ActionCatalog>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<Role> AuthorizationRoles => Set<Role>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<RoleGroup> RoleGroups => Set<RoleGroup>();
    public DbSet<RoleGroupMember> RoleGroupMembers => Set<RoleGroupMember>();
    public DbSet<Grant> Grants => Set<Grant>();
    public DbSet<Delegation> Delegations => Set<Delegation>();
    public DbSet<AuthorizationAuditEntry> AuthorizationAuditEntries => Set<AuthorizationAuditEntry>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
    }
}
