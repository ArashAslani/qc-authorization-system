using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using qc_authorization.Application.Common.Mappings;
using qc_authorization.Application.Authorization.Audit;
using qc_authorization.Application.Authorization.Commands.AssignAuthorizationRoleToUser;
using qc_authorization.Application.Common.Interfaces;
using qc_authorization.Domain.Authorization.Enums;
using qc_authorization.Application.Authorization.Commands.AssignPermissionToRole;
using qc_authorization.Application.Authorization.Commands.CreatePermission;
using qc_authorization.Application.Authorization.Commands.CreateRole;
using qc_authorization.Application.Authorization.Evaluation;
using qc_authorization.Application.Authorization.Queries.EvaluateAccess;
using qc_authorization.Domain.Authorization.Evaluation;
using qc_authorization.Domain.Authorization.Services;
using qc_authorization.Domain.Organization;
using qc_authorization.Infrastructure.Data;
using qc_authorization.Infrastructure.IntegrationTests.TestSupport;
using MediatR;
using NUnit.Framework;
using Shouldly;

namespace qc_authorization.Infrastructure.IntegrationTests.Authorization;

using qc_authorization.Tests.TestSupport;

[TestFixture]
public class IdentityAuthorizationIntegrationTests
{
    private ApplicationDbContext _context = null!;
    private IMediator _mediator = null!;
    private static readonly DateTimeOffset T0 = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    [SetUp]
    public async Task SetUp()
    {
        MappingConfig.RegisterMappings();

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"qc-id-auth-{Guid.NewGuid():N}")
            .Options;
        _context = new ApplicationDbContext(options);
        await _context.Database.EnsureCreatedAsync();

        _mediator = new ServiceCollection()
            .AddLogging()
            .AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<AssignAuthorizationRoleToUserCommand>())
            .AddScoped<IApplicationDbContext>(_ => _context)
            .AddTestCurrentUser()
            .AddAuthorizationEvaluationServices()
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
    public async Task Role_Assignment_Allows_Evaluation()
    {
        var permissionId = await _mediator.Send(new CreatePermissionCommand(
            "Personnel", "Personnel", "Read", "Read"));
        var roleId = await _mediator.Send(new CreateRoleCommand("HR_READER", "HR Reader"));
        await _mediator.Send(new AssignPermissionToRoleCommand(roleId, permissionId));
        await _mediator.Send(new AssignAuthorizationRoleToUserCommand(TestUsers.UserA, roleId, T0));

        var result = await _mediator.Send(new EvaluateAccessQuery(
            SubjectType.User, Guid.Empty, TestUsers.UserA, "Read", "Personnel", null, T0));

        result.Effect.ShouldBe("Allow");
        result.Trace.ApplicableCount.ShouldBeGreaterThan(0);
    }

    [Test]
    public async Task User_Without_Role_Is_Denied()
    {
        var permissionId = await _mediator.Send(new CreatePermissionCommand(
            "Personnel", "Personnel", "Read", "Read"));
        var roleId = await _mediator.Send(new CreateRoleCommand("HR_READER", "HR Reader"));
        await _mediator.Send(new AssignPermissionToRoleCommand(roleId, permissionId));

        var result = await _mediator.Send(new EvaluateAccessQuery(
            SubjectType.User, Guid.Empty, TestUsers.Unknown, "Read", "Personnel", null, T0));

        result.Effect.ShouldBe("Deny");
    }
}
