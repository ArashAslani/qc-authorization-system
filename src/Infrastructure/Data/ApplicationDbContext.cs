using System.Reflection;
using qc_authorization.Domain.Authorization;
using qc_authorization.Domain.Organization;
using Microsoft.EntityFrameworkCore;

namespace qc_authorization.Infrastructure.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<Personnel> Personnel => Set<Personnel>();
    public DbSet<Position> Positions => Set<Position>();
    public DbSet<PositionAssignment> PositionAssignments => Set<PositionAssignment>();

    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<Grant> Grants => Set<Grant>();
    public DbSet<Delegation> Delegations => Set<Delegation>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
    }
}
