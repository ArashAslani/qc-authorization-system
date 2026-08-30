using AccessManagement.Application.Abstractions;
using AccessManagement.Application.Authorization.Audit;
using AccessManagement.Application.Authorization.Commands.AssignRoleGroupToPosition;
using AccessManagement.Application.Authorization.Commands.AssignRoleGroupToUser;
using AccessManagement.Application.Authorization.Commands.CreatePermission;
using AccessManagement.Application.Authorization.Commands.CreateRole;
using AccessManagement.Application.Authorization.Commands.CreateRoleGroup;
using AccessManagement.Application.Authorization.Commands.RevokeRoleGroupFromPosition;
using AccessManagement.Application.Authorization.Commands.RevokeRoleGroupFromUser;
using AccessManagement.Application.Authorization.Commands.AddRoleToGroup;
using AccessManagement.Application.Authorization.Commands.AssignPermissionToRole;
using AccessManagement.Application.Authorization.Evaluation;
using AccessManagement.Infrastructure.IntegrationTests.TestSupport;
using AccessManagement.Application.Common.Interfaces;
using AccessManagement.Domain.Authorization;
using AccessManagement.Domain.Authorization.Enums;
using AccessManagement.Domain.Authorization.Evaluation;
using AccessManagement.Domain.Authorization.Services;
using AccessManagement.Domain.Organization;
using AccessManagement.Domain.Organization.Enums;
using AccessManagement.Infrastructure.Data;
using AccessManagement.Tests.TestSupport;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Shouldly;

namespace AccessManagement.Infrastructure.IntegrationTests.Authorization;

[TestFixture]
public class RoleGroupAssignmentTests
{
    private ApplicationDbContext _context = null!;
    private IMediator _mediator = null!;
    private ServiceProvider _services = null!;

    [SetUp]
    public async Task SetUp()
    {
        var dbName = $"qc-rolegroup-{Guid.NewGuid():N}";
        _services = new ServiceCollection()
            .AddLogging()
            .AddDbContext<ApplicationDbContext>(options => options.UseInMemoryDatabase(dbName))
            .AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<CreatePermissionCommand>())
            .AddTestCurrentUser(activeCompanyId: TestGuids.CompanyA)
            .AddAuthorizationEvaluationServices()
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
    public async Task AssignRoleGroupToUser_Materializes_All_Role_Permissions()
    {
        var readPermId = await _mediator.Send(new CreatePermissionCommand(
            "PERSONNEL", "Personnel", "READ", "Read"));
        var updatePermId = await _mediator.Send(new CreatePermissionCommand(
            "PERSONNEL", "Personnel", "UPDATE", "Update"));

        var roleAId = await _mediator.Send(new CreateRoleCommand("ROLE_A", "Role A"));
        var roleBId = await _mediator.Send(new CreateRoleCommand("ROLE_B", "Role B"));
        await _mediator.Send(new AssignPermissionToRoleCommand(roleAId, readPermId));
        await _mediator.Send(new AssignPermissionToRoleCommand(roleBId, updatePermId));

        var groupId = await _mediator.Send(new CreateRoleGroupCommand("HR_GROUP", "HR Group"));
        await _mediator.Send(new AddRoleToGroupCommand(groupId, roleAId));
        await _mediator.Send(new AddRoleToGroupCommand(groupId, roleBId));

        var externalUserId = Guid.NewGuid();
        await _mediator.Send(new AssignRoleGroupToUserCommand(
            externalUserId, groupId, DateTimeOffset.UtcNow.AddDays(-1)));

        var grants = await _context.Grants
            .Where(g => g.SubjectUserId == externalUserId && g.SourceType == SourceType.RoleGroup)
            .ToListAsync();

        grants.Count.ShouldBe(2);
        grants.ShouldAllBe(g => g.SourceId == groupId);
        grants.Select(g => g.PermissionId).ShouldContain(readPermId);
        grants.Select(g => g.PermissionId).ShouldContain(updatePermId);
    }

    [Test]
    public async Task RevokeRoleGroupFromUser_Removes_Materialized_Grants()
    {
        var permId = await _mediator.Send(new CreatePermissionCommand(
            "BOM", "BOM", "READ", "Read"));
        var roleId = await _mediator.Send(new CreateRoleCommand("BOM_READER", "BOM Reader"));
        await _mediator.Send(new AssignPermissionToRoleCommand(roleId, permId));

        var groupId = await _mediator.Send(new CreateRoleGroupCommand("BOM_GROUP", "BOM Group"));
        await _mediator.Send(new AddRoleToGroupCommand(groupId, roleId));

        var userId = Guid.NewGuid();
        await _mediator.Send(new AssignRoleGroupToUserCommand(userId, groupId, DateTimeOffset.UtcNow.AddDays(-1)));
        (await _context.Grants.CountAsync(g => g.SubjectUserId == userId)).ShouldBe(1);

        await _mediator.Send(new RevokeRoleGroupFromUserCommand(userId, groupId));
        (await _context.Grants.CountAsync(g => g.SubjectUserId == userId)).ShouldBe(0);
    }

    [Test]
    public async Task AssignRoleGroupToPosition_Materializes_Grants_For_Position()
    {
        var permId = await _mediator.Send(new CreatePermissionCommand(
            "CONTROL_PLAN", "Control Plan", "APPROVE", "Approve"));
        var roleId = await _mediator.Send(new CreateRoleCommand("QC_MGR", "QC Manager"));
        await _mediator.Send(new AssignPermissionToRoleCommand(roleId, permId));

        var groupId = await _mediator.Send(new CreateRoleGroupCommand("QC_GROUP", "QC Group"));
        await _mediator.Send(new AddRoleToGroupCommand(groupId, roleId));

        var position = Position.Create(TestGuids.CompanyA, "QC-MGR", "QC Manager");
        position.Id = TestGuids.PosA1;
        _context.Positions.Add(position);
        await _context.SaveChangesAsync();

        await _mediator.Send(new AssignRoleGroupToPositionCommand(
            position.Id, groupId, DateTimeOffset.UtcNow.AddDays(-1)));

        var grants = await _context.Grants
            .Where(g => g.SubjectType == SubjectType.Position && g.SubjectId == position.Id)
            .ToListAsync();

        grants.Count.ShouldBe(1);
        grants[0].SourceType.ShouldBe(SourceType.RoleGroup);
        grants[0].SourceId.ShouldBe(groupId);
        grants[0].PermissionId.ShouldBe(permId);
    }

    [Test]
    public async Task RevokeRoleGroupFromPosition_Removes_Materialized_Grants()
    {
        var permId = await _mediator.Send(new CreatePermissionCommand(
            "LABORATORY", "Laboratory", "READ", "Read"));
        var roleId = await _mediator.Send(new CreateRoleCommand("LAB_READER", "Lab Reader"));
        await _mediator.Send(new AssignPermissionToRoleCommand(roleId, permId));

        var groupId = await _mediator.Send(new CreateRoleGroupCommand("LAB_GROUP", "Lab Group"));
        await _mediator.Send(new AddRoleToGroupCommand(groupId, roleId));

        var position = Position.Create(TestGuids.CompanyA, "LAB-LEAD", "Lab Lead");
        position.Id = TestGuids.PosA2;
        _context.Positions.Add(position);
        await _context.SaveChangesAsync();

        await _mediator.Send(new AssignRoleGroupToPositionCommand(
            position.Id, groupId, DateTimeOffset.UtcNow.AddDays(-1)));
        await _mediator.Send(new RevokeRoleGroupFromPositionCommand(position.Id, groupId));

        (await _context.Grants.CountAsync(g => g.SubjectId == position.Id)).ShouldBe(0);
    }

    [Test]
    public async Task RoleGroup_User_Grant_Applies_In_Evaluation()
    {
        var permId = await _mediator.Send(new CreatePermissionCommand(
            "REPORT", "Report", "VIEW", "View"));
        var roleId = await _mediator.Send(new CreateRoleCommand("REPORTER", "Reporter"));
        await _mediator.Send(new AssignPermissionToRoleCommand(roleId, permId));

        var groupId = await _mediator.Send(new CreateRoleGroupCommand("REPORT_GROUP", "Report Group"));
        await _mediator.Send(new AddRoleToGroupCommand(groupId, roleId));

        var userId = Guid.NewGuid();
        await _mediator.Send(new AssignRoleGroupToUserCommand(userId, groupId, DateTimeOffset.UtcNow.AddDays(-1)));

        var evaluator = _services.GetRequiredService<IAccessEvaluator>();
        var decision = await evaluator.EvaluateAsync(AccessRequest.ForUser(
            userId, "VIEW", "REPORT", null, DateTimeOffset.UtcNow));

        decision.Effect.ShouldBe(Effect.Allow);
    }
}
