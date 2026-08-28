using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using qc_authorization.Application.Authorization.Commands.CreatePermission;
using qc_authorization.Application.Authorization.Commands.CreateRole;
using qc_authorization.Application.Authorization.Commands.CreateRoleGroup;
using qc_authorization.Application.Authorization.Commands.AddRoleToGroup;
using qc_authorization.Application.Authorization.Commands.AssignPermissionToRole;
using qc_authorization.Application.Common.Interfaces;
using qc_authorization.Domain.Organization;
using qc_authorization.Infrastructure.Data;
using MediatR;
using NUnit.Framework;
using Shouldly;

namespace qc_authorization.Infrastructure.IntegrationTests.Authorization;

[TestFixture]
public class AccessDefinitionIntegrationTests
{
    private ApplicationDbContext _context = null!;
    private IMediator _mediator = null!;

    [SetUp]
    public async Task SetUp()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"qc-access-def-{Guid.NewGuid():N}")
            .Options;
        _context = new ApplicationDbContext(options);
        await _context.Database.EnsureCreatedAsync();

        _mediator = new ServiceCollection()
            .AddLogging()
            .AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<CreatePermissionCommand>())
            .AddSingleton<PositionHierarchyService>()
            .AddScoped<IApplicationDbContext>(_ => _context)
            .BuildServiceProvider()
            .GetRequiredService<IMediator>();
    }

    [TearDown]
    public async Task TearDown()
    {
        await _context.Database.EnsureDeletedAsync();
        await _context.DisposeAsync();
    }

    [Test]
    public async Task Can_Create_Permission_From_Catalog()
    {
        var id = await _mediator.Send(new CreatePermissionCommand(
            "Personnel", "Personnel", "Read", "Read"));

        var p = await _context.Permissions.SingleAsync(x => x.Id == id);
        p.Code.ShouldBe("PERSONNEL.READ");
        p.Resource.ShouldBe("PERSONNEL");
    }

    [Test]
    public async Task Can_Create_Role_And_Assign_Permission()
    {
        var permissionId = await _mediator.Send(new CreatePermissionCommand(
            "Personnel", "Personnel", "Update", "Update"));
        var roleId = await _mediator.Send(new CreateRoleCommand("HR_MANAGER", "HR Manager"));

        await _mediator.Send(new AssignPermissionToRoleCommand(roleId, permissionId));

        (await _context.RolePermissions.CountAsync()).ShouldBe(1);
    }

    [Test]
    public async Task Can_Create_RoleGroup_And_Add_Role()
    {
        var roleId = await _mediator.Send(new CreateRoleCommand("HR_SPECIALIST", "HR Specialist"));
        var groupId = await _mediator.Send(new CreateRoleGroupCommand("HR_GROUP", "HR Group"));

        await _mediator.Send(new AddRoleToGroupCommand(groupId, roleId));

        (await _context.RoleGroupMembers.CountAsync()).ShouldBe(1);
    }
}
