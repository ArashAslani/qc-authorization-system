using Microsoft.Extensions.DependencyInjection;
using qc_authorization.Application.Authorization.Audit;
using qc_authorization.Application.Authorization.Commands.CreateDelegation;
using qc_authorization.Application.Authorization.Commands.RevokeDelegation;
using qc_authorization.Application.Authorization.Delegation;
using qc_authorization.Application.Authorization.Evaluation;
using qc_authorization.Application.Common.Interfaces;
using qc_authorization.Application.UnitTests.TestSupport;
using qc_authorization.Domain.Authorization;
using qc_authorization.Domain.Authorization.Enums;
using qc_authorization.Domain.Authorization.Evaluation;
using qc_authorization.Domain.Authorization.Exceptions;
using qc_authorization.Domain.Authorization.Services;
using qc_authorization.Domain.Authorization.ValueObjects;
using qc_authorization.Domain.Organization;
using qc_authorization.Infrastructure.Data;
using MediatR;
using NUnit.Framework;
using Shouldly;

namespace qc_authorization.Application.UnitTests.Authorization;

[TestFixture]
public class DelegationEvaluationTests
{
    private ApplicationDbContext _context = null!;
    private AccessEvaluator _evaluator = null!;
    private IMediator _mediator = null!;
    private Permission _perm = null!;
    private static readonly DateTimeOffset T0 = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    [SetUp]
    public async Task SetUp()
    {
        (_context, _evaluator) = AuthorizationTestContext.Create();
        await _context.Database.EnsureCreatedAsync();

        _perm = Permission.Create("PERSONNEL.READ", "Personnel", "Read");
        _context.Permissions.Add(_perm);
        await _context.SaveChangesAsync();

        _mediator = CreateMediator();
    }

    [TearDown]
    public async Task TearDown()
    {
        await _context.Database.EnsureDeletedAsync();
        await _context.DisposeAsync();
    }

    [Test]
    public async Task Valid_Delegation_Allows_Delegate()
    {
        _context.Grants.Add(GrantForUser(TestUsers.UserA, Effect.Allow));
        await _context.SaveChangesAsync();

        await _mediator.Send(new CreateDelegationCommand(TestUsers.UserA, TestUsers.UserB, _perm.Id, T0, T0.AddDays(7)));

        var decision = await Evaluate(TestUsers.UserB);
        decision.Effect.ShouldBe(Effect.Allow);
        decision.Trace.ApplicableGrants.ShouldContain(g => g.SourceType == SourceType.Delegation);
    }

    [Test]
    public async Task Expired_Delegation_Denies()
    {
        _context.Grants.Add(GrantForUser(TestUsers.UserA, Effect.Allow));
        await _context.SaveChangesAsync();

        await _mediator.Send(new CreateDelegationCommand(
            TestUsers.UserA, TestUsers.UserB, _perm.Id, T0.AddDays(-10), T0.AddDays(-1)));

        var decision = await Evaluate(TestUsers.UserB);
        decision.Effect.ShouldBe(Effect.Deny);
    }

    [Test]
    public async Task Subset_Violation_Rejects_Delegation()
    {
        Should.Throw<AuthorizationDomainException>(async () =>
            await _mediator.Send(new CreateDelegationCommand(TestUsers.UserA, TestUsers.UserB, _perm.Id, T0, null)));
    }

    [Test]
    public async Task Non_Delegable_Parent_Blocks_Chain()
    {
        _context.Grants.Add(GrantForUser(TestUsers.UserA, Effect.Allow));
        await _context.SaveChangesAsync();

        var parentId = await _mediator.Send(new CreateDelegationCommand(
            TestUsers.UserA, TestUsers.UserB, _perm.Id, T0, null, Delegable: false));

        Should.Throw<AuthorizationDomainException>(async () =>
            await _mediator.Send(new CreateDelegationCommand(
                TestUsers.UserB, TestUsers.UserC, _perm.Id, T0, null, ParentDelegationId: parentId)));
    }

    [Test]
    public async Task Revoked_Delegation_Excluded_From_Evaluation()
    {
        _context.Grants.Add(GrantForUser(TestUsers.UserA, Effect.Allow));
        await _context.SaveChangesAsync();

        var delegationId = await _mediator.Send(new CreateDelegationCommand(TestUsers.UserA, TestUsers.UserB, _perm.Id, T0, null));
        (await Evaluate(TestUsers.UserB)).Effect.ShouldBe(Effect.Allow);

        await _mediator.Send(new RevokeDelegationCommand(delegationId));
        (await Evaluate(TestUsers.UserB)).Effect.ShouldBe(Effect.Deny);
    }

    [Test]
    public async Task Trace_Shows_Delegation_Source()
    {
        _context.Grants.Add(GrantForUser(TestUsers.UserA, Effect.Allow));
        await _context.SaveChangesAsync();

        await _mediator.Send(new CreateDelegationCommand(TestUsers.UserA, TestUsers.UserB, _perm.Id, T0, null));

        var decision = await Evaluate(TestUsers.UserB);
        decision.Trace.CandidateGrants.ShouldContain(g => g.SourceType == SourceType.Delegation);
    }

    private Task<AccessDecision> Evaluate(Guid userId) =>
        _evaluator.EvaluateAsync(AccessRequest.ForUser(userId, "Read", "Personnel", null, T0));

    private Grant GrantForUser(Guid userId, Effect effect) =>
        Grant.CreateForUser(
            userId,
            _perm.Id,
            SourceType.User,
            0,
            effect,
            T0.AddDays(-30),
            null,
            SourcePriority.IndividualOverride);

    private IMediator CreateMediator()
    {
        var hierarchy = new PositionHierarchyService();
        var applicability = new GrantApplicabilityService(hierarchy);
        var engine = new AccessEvaluationEngine();

        return new ServiceCollection()
            .AddLogging()
            .AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<CreateDelegationCommand>())
            .AddSingleton(hierarchy)
            .AddSingleton(applicability)
            .AddSingleton(engine)
            .AddScoped<IApplicationDbContext>(_ => _context)
            .AddScoped<IAuthorizationAuditService, AuthorizationAuditService>()
            .AddScoped<ICandidateGrantResolver, PositionAwareCandidateGrantResolver>()
            .AddScoped<IAccessEvaluator, AccessEvaluator>()
            .AddScoped<IDelegationSubsetPolicy, DelegationSubsetPolicy>()
            .BuildServiceProvider()
            .GetRequiredService<IMediator>();
    }
}
