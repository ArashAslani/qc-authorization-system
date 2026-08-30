using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using AccessManagement.Application.Abstractions;
using AccessManagement.Application.Common.Mappings;
using AccessManagement.Application.Authorization.Audit;
using AccessManagement.Application.Authorization.Commands.AssignAuthorizationRoleToUser;
using AccessManagement.Application.Common.Interfaces;
using AccessManagement.Domain.Authorization.Enums;
using AccessManagement.Application.Authorization.Commands.AssignPermissionToRole;
using AccessManagement.Application.Authorization.Commands.CreatePermission;
using AccessManagement.Application.Authorization.Commands.CreateRole;
using AccessManagement.Application.Authorization.Evaluation;
using AccessManagement.Application.Authorization.Queries.EvaluateAccess;
using AccessManagement.Domain.Authorization.Evaluation;
using AccessManagement.Domain.Authorization.Services;
using AccessManagement.Domain.Organization;
using AccessManagement.Infrastructure.Data;
using AccessManagement.Infrastructure.IntegrationTests.TestSupport;
using MediatR;
using NUnit.Framework;
using Shouldly;

namespace AccessManagement.Infrastructure.IntegrationTests.Authorization;

using AccessManagement.Tests.TestSupport;

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
            TestUsers.UserA, "PERSONNEL.READ", When: T0));

        result.Allowed.ShouldBeTrue();
    }

    [Test]
    public async Task User_Without_Role_Is_Denied()
    {
        var permissionId = await _mediator.Send(new CreatePermissionCommand(
            "Personnel", "Personnel", "Read", "Read"));
        var roleId = await _mediator.Send(new CreateRoleCommand("HR_READER", "HR Reader"));
        await _mediator.Send(new AssignPermissionToRoleCommand(roleId, permissionId));

        var result = await _mediator.Send(new EvaluateAccessQuery(
            TestUsers.Unknown, "PERSONNEL.READ", When: T0));

        result.Allowed.ShouldBeFalse();
    }
}
