using AccessManagement.Application.Authorization.Audit;
using AccessManagement.Application.Authorization.Commands.CreateDelegation;
using AccessManagement.Application.Authorization.Commands.CreateGrant;
using AccessManagement.Application.Authorization.Commands.CreatePermission;
using AccessManagement.Application.Authorization.Commands.RevokeDelegation;
using AccessManagement.Application.Authorization.Commands.RevokeGrant;
using AccessManagement.Application.Authorization.Delegation;
using AccessManagement.Application.Authorization.Evaluation;
using AccessManagement.Application.Authorization.Queries.EvaluateAccessForSubject;
using AccessManagement.Application.Authorization.Queries.GetDelegationById;
using AccessManagement.Application.Authorization.Queries.GetDelegations;
using AccessManagement.Application.Authorization.Queries.GetGrantById;
using AccessManagement.Application.Authorization.Queries.GetGrants;
using AccessManagement.Application.Common.Interfaces;
using AccessManagement.Domain.Authorization.Enums;
using AccessManagement.Domain.Authorization.Evaluation;
using AccessManagement.Domain.Authorization.Services;
using AccessManagement.Domain.Authorization.ValueObjects;
using AccessManagement.Domain.Organization;
using AccessManagement.Infrastructure.Data;
using AccessManagement.Infrastructure.IntegrationTests.TestSupport;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Shouldly;

namespace AccessManagement.Infrastructure.IntegrationTests.Authorization;

using AccessManagement.Tests.TestSupport;

[TestFixture]
public class GrantAndDelegationQueryIntegrationTests
{
    private ApplicationDbContext _context = null!;
    private IMediator _mediator = null!;
    private ServiceProvider _services = null!;

    [SetUp]
    public async Task SetUp()
    {
        var dbName = $"qc-grant-del-query-{Guid.NewGuid():N}";
        _services = new ServiceCollection()
            .AddLogging()
            .AddDbContext<ApplicationDbContext>(options => options.UseInMemoryDatabase(dbName))
            .AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<GetGrantsQuery>())
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
    public async Task Can_Query_Grants_And_Details()
    {
        var permId = await _mediator.Send(new CreatePermissionCommand("INSPECTION", "Inspection", "SIGN", "Sign"));
        var userId = Guid.NewGuid();

        var grantId = await _mediator.Send(new CreateGrantCommand(
            SubjectType.User,
            Guid.Empty,
            userId,
            permId,
            "INSPECTION",
            null,
            null,
            Effect.Allow,
            SourceType.User,
            Guid.Empty,
            DateTimeOffset.UtcNow.AddDays(-1),
            DateTimeOffset.UtcNow.AddDays(10),
            100));

        var grants = await _mediator.Send(new GetGrantsQuery(SubjectUserId: userId));
        grants.Count.ShouldBe(1);
        grants[0].Id.ShouldBe(grantId);
        grants[0].PermissionCode.ShouldBe("INSPECTION.SIGN");
        grants[0].IsActive.ShouldBeTrue();

        var details = await _mediator.Send(new GetGrantByIdQuery(grantId));
        details.Id.ShouldBe(grantId);
        details.Effect.ShouldBe(Effect.Allow);
    }

    [Test]
    public async Task Can_Query_Delegations_And_Revoke()
    {
        var permId = await _mediator.Send(new CreatePermissionCommand("TASK", "Task", "EXECUTE", "Execute"));
        var delegator = Guid.NewGuid();
        var @delegate = Guid.NewGuid();

        // Seed delegator grant so subset policy passes
        await _mediator.Send(new CreateGrantCommand(
            SubjectType.User,
            Guid.Empty,
            delegator,
            permId,
            "TASK",
            null,
            null,
            Effect.Allow,
            SourceType.User,
            Guid.Empty,
            DateTimeOffset.UtcNow.AddDays(-5),
            DateTimeOffset.UtcNow.AddDays(30),
            100));

        var delId = await _mediator.Send(new CreateDelegationCommand(
            delegator,
            @delegate,
            permId,
            DateTimeOffset.UtcNow.AddDays(-1),
            DateTimeOffset.UtcNow.AddDays(5)));

        var delegations = await _mediator.Send(new GetDelegationsQuery(DelegateUserId: @delegate));
        delegations.Count.ShouldBe(1);
        delegations[0].Id.ShouldBe(delId);
        delegations[0].IsActive.ShouldBeTrue();

        var details = await _mediator.Send(new GetDelegationByIdQuery(delId));
        details.Id.ShouldBe(delId);
        details.IsRevoked.ShouldBeFalse();

        await _mediator.Send(new RevokeDelegationCommand(delId));
        var updatedDetails = await _mediator.Send(new GetDelegationByIdQuery(delId));
        updatedDetails.IsRevoked.ShouldBeTrue();
        updatedDetails.IsActive.ShouldBeFalse();
    }

    [Test]
    public async Task Can_Simulate_Evaluation_For_Subject()
    {
        var permId = await _mediator.Send(new CreatePermissionCommand("REPORT", "Report", "GENERATE", "Generate"));
        var userId = Guid.NewGuid();

        await _mediator.Send(new CreateGrantCommand(
            SubjectType.User,
            Guid.Empty,
            userId,
            permId,
            "REPORT",
            null,
            null,
            Effect.Allow,
            SourceType.User,
            Guid.Empty,
            DateTimeOffset.UtcNow.AddDays(-1),
            null,
            100));

        var decision = await _mediator.Send(new EvaluateAccessForSubjectQuery(
            SubjectType.User,
            Guid.Empty,
            userId,
            "GENERATE",
            "REPORT",
            null,
            DateTimeOffset.UtcNow));

        decision.Allowed.ShouldBeTrue();
    }
}
