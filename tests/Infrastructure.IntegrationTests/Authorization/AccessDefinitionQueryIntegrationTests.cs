using qc_authorization.Application.Authorization.Audit;
using qc_authorization.Application.Authorization.Commands.AddRoleToGroup;
using qc_authorization.Application.Authorization.Commands.AssignAuthorizationRoleToUser;
using qc_authorization.Application.Authorization.Commands.AssignPermissionToRole;
using qc_authorization.Application.Authorization.Commands.CreatePermission;
using qc_authorization.Application.Authorization.Commands.CreateRole;
using qc_authorization.Application.Authorization.Commands.CreateRoleGroup;
using qc_authorization.Application.Authorization.Commands.RemovePermissionFromRole;
using qc_authorization.Application.Authorization.Commands.RemoveRoleFromGroup;
using qc_authorization.Application.Authorization.Queries.GetActionCatalogs;
using qc_authorization.Application.Authorization.Queries.GetPermissionById;
using qc_authorization.Application.Authorization.Queries.GetPermissions;
using qc_authorization.Application.Authorization.Queries.GetResourceCatalogs;
using qc_authorization.Application.Authorization.Queries.GetRoleById;
using qc_authorization.Application.Authorization.Queries.GetRoleGroupById;
using qc_authorization.Application.Authorization.Queries.GetRoleGroups;
using qc_authorization.Application.Authorization.Queries.GetRoles;
using qc_authorization.Application.Authorization.Queries.GetUserRoles;
using qc_authorization.Application.Common.Interfaces;
using qc_authorization.Domain.Organization;
using qc_authorization.Infrastructure.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Shouldly;

namespace qc_authorization.Infrastructure.IntegrationTests.Authorization;

using qc_authorization.Tests.TestSupport;

[TestFixture]
public class AccessDefinitionQueryIntegrationTests
{
    private ApplicationDbContext _context = null!;
    private IMediator _mediator = null!;
    private ServiceProvider _services = null!;

    [SetUp]
    public async Task SetUp()
    {
        var dbName = $"qc-access-def-query-{Guid.NewGuid():N}";
        _services = new ServiceCollection()
            .AddLogging()
            .AddDbContext<ApplicationDbContext>(options => options.UseInMemoryDatabase(dbName))
            .AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<GetPermissionsQuery>())
            .AddSingleton<PositionHierarchyService>()
            .AddScoped<IAuthorizationAuditService, AuthorizationAuditService>()
            .AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>())
            .BuildServiceProvider();

        _context = _services.GetRequiredService<ApplicationDbContext>();
        await _context.Database.EnsureCreatedAsync();
        _mediator = _services.GetRequiredService<IMediator>();
    }

    [TearDown]
    public async Task TearDown()
    {
        await _context.Database.EnsureDeletedAsync();
        await _context.DisposeAsync();
        await _services.DisposeAsync();
    }

    [Test]
    public async Task Can_Query_Catalogs_And_Permissions()
    {
        var p1 = await _mediator.Send(new CreatePermissionCommand("DOC", "Document", "VIEW", "View"));
        var p2 = await _mediator.Send(new CreatePermissionCommand("DOC", "Document", "EDIT", "Edit"));

        var resCatalogs = await _mediator.Send(new GetResourceCatalogsQuery());
        resCatalogs.Count.ShouldBe(1);
        resCatalogs[0].Code.ShouldBe("DOC");

        var actCatalogs = await _mediator.Send(new GetActionCatalogsQuery());
        actCatalogs.Count.ShouldBe(2);

        var permissions = await _mediator.Send(new GetPermissionsQuery(Resource: "DOC"));
        permissions.Count.ShouldBe(2);

        var details = await _mediator.Send(new GetPermissionByIdQuery(p1));
        details.Id.ShouldBe(p1);
        details.Code.ShouldBe("DOC.VIEW");
    }

    [Test]
    public async Task Can_Manage_Roles_Permissions_And_Groups()
    {
        var permId = await _mediator.Send(new CreatePermissionCommand("AUDIT", "Audit", "APPROVE", "Approve"));
        var roleId = await _mediator.Send(new CreateRoleCommand("AUDITOR", "Lead Auditor"));
        var groupId = await _mediator.Send(new CreateRoleGroupCommand("AUDIT_GRP", "Audit Group"));

        await _mediator.Send(new AssignPermissionToRoleCommand(roleId, permId));
        await _mediator.Send(new AddRoleToGroupCommand(groupId, roleId));

        var roles = await _mediator.Send(new GetRolesQuery());
        roles.Count.ShouldBe(1);
        roles[0].PermissionCount.ShouldBe(1);

        var roleDetails = await _mediator.Send(new GetRoleByIdQuery(roleId));
        roleDetails.Permissions.Count.ShouldBe(1);
        roleDetails.Groups.Count.ShouldBe(1);

        var groups = await _mediator.Send(new GetRoleGroupsQuery());
        groups.Count.ShouldBe(1);
        groups[0].MemberRoleCount.ShouldBe(1);

        var groupDetails = await _mediator.Send(new GetRoleGroupByIdQuery(groupId));
        groupDetails.MemberRoles.Count.ShouldBe(1);

        // Test removing permission and removing role from group
        await _mediator.Send(new RemovePermissionFromRoleCommand(roleId, permId));
        (await _context.RolePermissions.CountAsync()).ShouldBe(0);

        await _mediator.Send(new RemoveRoleFromGroupCommand(groupId, roleId));
        (await _context.RoleGroupMembers.CountAsync()).ShouldBe(0);
    }

    [Test]
    public async Task Can_Query_User_Assigned_Roles()
    {
        var userId = Guid.NewGuid();
        var permId = await _mediator.Send(new CreatePermissionCommand("INVOICE", "Invoice", "PAY", "Pay"));
        var roleId = await _mediator.Send(new CreateRoleCommand("FINANCE", "Finance Officer"));
        await _mediator.Send(new AssignPermissionToRoleCommand(roleId, permId));

        var from = DateTimeOffset.UtcNow.AddDays(-1);
        await _mediator.Send(new AssignAuthorizationRoleToUserCommand(userId, roleId, from));

        var userRoles = await _mediator.Send(new GetUserRolesQuery(userId));
        userRoles.Count.ShouldBe(1);
        userRoles[0].RoleId.ShouldBe(roleId);
        userRoles[0].RoleCode.ShouldBe("FINANCE");
        userRoles[0].IsActive.ShouldBeTrue();
    }
}
