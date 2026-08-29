using qc_authorization.Application.Authorization.Commands.AddRoleToGroup;
using qc_authorization.Application.Authorization.Commands.AssignAuthorizationRoleToPosition;
using qc_authorization.Application.Authorization.Commands.AssignAuthorizationRoleToUser;
using qc_authorization.Application.Authorization.Commands.AssignPermissionToRole;
using qc_authorization.Application.Authorization.Commands.AssignRoleGroupToUser;
using qc_authorization.Application.Authorization.Commands.CreatePermission;
using qc_authorization.Application.Authorization.Commands.CreateRole;
using qc_authorization.Application.Authorization.Commands.CreateRoleGroup;
using qc_authorization.Application.Authorization.Commands.RevokeAuthorizationRoleFromPosition;
using qc_authorization.Application.Authorization.Commands.UpdateRole;
using qc_authorization.Application.Authorization.Commands.UpdateRoleGroup;
using qc_authorization.Application.Authorization.Evaluation;
using qc_authorization.Application.Common.Interfaces;
using qc_authorization.Domain.Authorization.Enums;
using qc_authorization.Domain.Authorization.Evaluation;
using qc_authorization.Domain.Organization;
using qc_authorization.Infrastructure.Data;
using qc_authorization.Infrastructure.IntegrationTests.TestSupport;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Shouldly;

namespace qc_authorization.Infrastructure.IntegrationTests.Authorization;

using qc_authorization.Tests.TestSupport;

[TestFixture]
public class UsAccess01BackendTests
{
    private ApplicationDbContext _context = null!;
    private IMediator _mediator = null!;
    private ServiceProvider _services = null!;
    private static readonly DateTimeOffset T0 = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    [SetUp]
    public async Task SetUp()
    {
        var dbName = $"qc-us-access-{Guid.NewGuid():N}";
        _services = new ServiceCollection()
            .AddLogging()
            .AddDbContext<ApplicationDbContext>(options => options.UseInMemoryDatabase(dbName))
            .AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<CreatePermissionCommand>())
            .AddTestCurrentUser()
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
    public async Task AssignRoleGroup_Without_Member_Roles_Throws()
    {
        var groupId = await _mediator.Send(new CreateRoleGroupCommand("EMPTY_GROUP", "Empty Group"));

        await Should.ThrowAsync<InvalidOperationException>(async () =>
            await _mediator.Send(new AssignRoleGroupToUserCommand(Guid.NewGuid(), groupId, T0)));
    }

    [Test]
    public async Task AssignRoleToPosition_Materializes_And_Evaluates()
    {
        var permId = await _mediator.Send(new CreatePermissionCommand(
            "INSPECTION", "Inspection", "SIGN", "Sign"));
        var roleId = await _mediator.Send(new CreateRoleCommand("INSPECTOR", "Inspector"));
        await _mediator.Send(new AssignPermissionToRoleCommand(roleId, permId));

        var position = Position.Create(TestGuids.CompanyA, "INSP-1", "Inspector");
        position.Id = TestGuids.PosA1;
        _context.Positions.Add(position);
        await _context.SaveChangesAsync();

        await _mediator.Send(new AssignAuthorizationRoleToPositionCommand(
            position.Id, roleId, T0.AddDays(-1)));

        var grants = await _context.Grants
            .Where(g => g.SubjectType == SubjectType.Position && g.SubjectId == position.Id)
            .ToListAsync();
        grants.Count.ShouldBe(1);
        grants[0].SourceType.ShouldBe(SourceType.Role);

        var evaluator = _services.GetRequiredService<IAccessEvaluator>();
        var decision = await evaluator.EvaluateAsync(new AccessRequest(
            SubjectType.Position, position.Id, null, "Sign", "INSPECTION", null, T0));

        decision.Effect.ShouldBe(Effect.Allow);
    }

    [Test]
    public async Task RevokeRoleFromPosition_Removes_Grants()
    {
        var permId = await _mediator.Send(new CreatePermissionCommand(
            "BOM", "BOM", "READ", "Read"));
        var roleId = await _mediator.Send(new CreateRoleCommand("BOM_READER", "BOM Reader"));
        await _mediator.Send(new AssignPermissionToRoleCommand(roleId, permId));

        var position = Position.Create(TestGuids.CompanyA, "BOM-1", "BOM Lead");
        position.Id = TestGuids.PosA2;
        _context.Positions.Add(position);
        await _context.SaveChangesAsync();

        await _mediator.Send(new AssignAuthorizationRoleToPositionCommand(
            position.Id, roleId, T0.AddDays(-1)));
        await _mediator.Send(new RevokeAuthorizationRoleFromPositionCommand(position.Id, roleId));

        (await _context.Grants.CountAsync(g => g.SubjectId == position.Id)).ShouldBe(0);
    }

    [Test]
    public async Task Inactive_Role_Excluded_From_Evaluation()
    {
        var permId = await _mediator.Send(new CreatePermissionCommand(
            "CALIBRATION", "Calibration", "READ", "Read"));
        var roleId = await _mediator.Send(new CreateRoleCommand("CAL_READER", "Calibration Reader"));
        await _mediator.Send(new AssignPermissionToRoleCommand(roleId, permId));

        var userId = Guid.NewGuid();
        await _mediator.Send(new AssignAuthorizationRoleToUserCommand(userId, roleId, T0.AddDays(-1)));
        await _mediator.Send(new UpdateRoleCommand(roleId, "Calibration Reader", null, CatalogStatus.Inactive));

        var evaluator = _services.GetRequiredService<IAccessEvaluator>();
        var decision = await evaluator.EvaluateAsync(new AccessRequest(
            SubjectType.User, Guid.Empty, userId, "Read", "CALIBRATION", null, T0));

        decision.Effect.ShouldBe(Effect.Deny);
    }

    [Test]
    public async Task Inactive_RoleGroup_Excluded_From_Evaluation()
    {
        var permId = await _mediator.Send(new CreatePermissionCommand(
            "SAMPLING", "Sampling", "READ", "Read"));
        var roleId = await _mediator.Send(new CreateRoleCommand("SAMPLE_READER", "Sample Reader"));
        await _mediator.Send(new AssignPermissionToRoleCommand(roleId, permId));

        var groupId = await _mediator.Send(new CreateRoleGroupCommand("SAMPLE_GROUP", "Sample Group"));
        await _mediator.Send(new AddRoleToGroupCommand(groupId, roleId));

        var userId = Guid.NewGuid();
        await _mediator.Send(new AssignRoleGroupToUserCommand(userId, groupId, T0.AddDays(-1)));
        await _mediator.Send(new UpdateRoleGroupCommand(groupId, "Sample Group", null, CatalogStatus.Inactive));

        var evaluator = _services.GetRequiredService<IAccessEvaluator>();
        var decision = await evaluator.EvaluateAsync(new AccessRequest(
            SubjectType.User, Guid.Empty, userId, "Read", "SAMPLING", null, T0));

        decision.Effect.ShouldBe(Effect.Deny);
    }

    [Test]
    public async Task Assign_Inactive_RoleGroup_Throws()
    {
        var permId = await _mediator.Send(new CreatePermissionCommand(
            "TRAINING", "Training", "READ", "Read"));
        var roleId = await _mediator.Send(new CreateRoleCommand("TRAIN_READER", "Training Reader"));
        await _mediator.Send(new AssignPermissionToRoleCommand(roleId, permId));

        var groupId = await _mediator.Send(new CreateRoleGroupCommand("TRAIN_GROUP", "Training Group"));
        await _mediator.Send(new AddRoleToGroupCommand(groupId, roleId));
        await _mediator.Send(new UpdateRoleGroupCommand(groupId, "Training Group", null, CatalogStatus.Inactive));

        await Should.ThrowAsync<Domain.Authorization.Exceptions.AuthorizationDomainException>(async () =>
            await _mediator.Send(new AssignRoleGroupToUserCommand(Guid.NewGuid(), groupId, T0)));
    }
}
