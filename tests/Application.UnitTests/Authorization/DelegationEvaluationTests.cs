using Microsoft.Extensions.DependencyInjection;
using AccessManagement.Application.Authorization.Audit;
using AccessManagement.Application.Authorization.Commands.CreateDelegation;
using AccessManagement.Application.Authorization.Commands.RevokeDelegation;
using AccessManagement.Application.Authorization.Delegation;
using AccessManagement.Application.Authorization.Evaluation;
using AccessManagement.Application.Common.Interfaces;
using AccessManagement.Application.UnitTests.TestSupport;
using AccessManagement.Domain.Authorization;
using AccessManagement.Domain.Authorization.Enums;
using AccessManagement.Domain.Authorization.Evaluation;
using AccessManagement.Domain.Authorization.Exceptions;
using AccessManagement.Domain.Authorization.Services;
using AccessManagement.Domain.Authorization.ValueObjects;
using AccessManagement.Domain.Organization;
using AccessManagement.Infrastructure.Data;
using MediatR;
using NUnit.Framework;
using Shouldly;

namespace AccessManagement.Application.UnitTests.Authorization;

using AccessManagement.Tests.TestSupport;

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

        _mediator = AuthorizationTestContext.CreateMediatorServices(_context).GetRequiredService<IMediator>();
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
        decision.Allowed.ShouldBeTrue();
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

        await _mediator.Send(new RevokeDelegationCommand(delegationId, TestUsers.UserA));
        (await Evaluate(TestUsers.UserB)).Effect.ShouldBe(Effect.Deny);
    }

    [Test]
    public async Task Trace_Shows_Delegation_Source()
    {
        _context.Grants.Add(GrantForUser(TestUsers.UserA, Effect.Allow));
        await _context.SaveChangesAsync();

        await _mediator.Send(new CreateDelegationCommand(TestUsers.UserA, TestUsers.UserB, _perm.Id, T0, null));

        var decision = await Evaluate(TestUsers.UserB);
        decision.Allowed.ShouldBeTrue();
    }

    private Task<AccessDecision> Evaluate(Guid userId) =>
        _evaluator.EvaluateAsync(AccessRequest.ForUser(userId, "Read", "Personnel", null, T0));

    private Grant GrantForUser(Guid userId, Effect effect) =>
        Grant.CreateForUser(
            userId,
            _perm.Id,
            SourceType.User,
            Guid.Empty,
            effect,
            T0.AddDays(-30),
            null,
            SourcePriority.IndividualOverride);
}
