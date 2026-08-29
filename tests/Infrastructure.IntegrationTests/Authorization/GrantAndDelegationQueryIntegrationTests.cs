using qc_authorization.Application.Authorization.Audit;
using qc_authorization.Application.Authorization.Commands.CreateDelegation;
using qc_authorization.Application.Authorization.Commands.CreateGrant;
using qc_authorization.Application.Authorization.Commands.CreatePermission;
using qc_authorization.Application.Authorization.Commands.RevokeDelegation;
using qc_authorization.Application.Authorization.Commands.RevokeGrant;
using qc_authorization.Application.Authorization.Delegation;
using qc_authorization.Application.Authorization.Evaluation;
using qc_authorization.Application.Authorization.Queries.EvaluateAccessForSubject;
using qc_authorization.Application.Authorization.Queries.GetDelegationById;
using qc_authorization.Application.Authorization.Queries.GetDelegations;
using qc_authorization.Application.Authorization.Queries.GetGrantById;
using qc_authorization.Application.Authorization.Queries.GetGrants;
using qc_authorization.Application.Common.Interfaces;
using qc_authorization.Domain.Authorization.Enums;
using qc_authorization.Domain.Authorization.Evaluation;
using qc_authorization.Domain.Authorization.Services;
using qc_authorization.Domain.Authorization.ValueObjects;
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
            .AddSingleton<PositionHierarchyService>()
            .AddSingleton<GrantApplicabilityService>()
            .AddSingleton<AccessEvaluationEngine>()
            .AddSingleton<ICurrentUser>(new StaticCurrentUser(activeCompanyId: TestGuids.CompanyA))
            .AddScoped<ICandidateGrantResolver, PositionAwareCandidateGrantResolver>()
            .AddScoped<IAccessEvaluator, AccessEvaluator>()
            .AddScoped<IDelegationSubsetPolicy, DelegationSubsetPolicy>()
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
            ScopeKind.Unbounded,
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
            ScopeKind.Unbounded,
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
            ScopeKind.Unbounded,
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

        decision.Effect.ShouldBe("Allow");
        decision.Trace.ShouldNotBeNull();
        decision.Trace.CandidateGrants.Count.ShouldBe(1);
        decision.Trace.ApplicableGrants.Count.ShouldBe(1);
    }
}
