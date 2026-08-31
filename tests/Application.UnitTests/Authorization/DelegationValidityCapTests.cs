using AccessManagement.Application.Authorization.Commands.CreateDelegation;
using AccessManagement.Application.UnitTests.TestSupport;
using AccessManagement.Domain.Authorization;
using AccessManagement.Domain.Authorization.Enums;
using AccessManagement.Domain.Authorization.ValueObjects;
using AccessManagement.Infrastructure.Data;
using AccessManagement.Tests.TestSupport;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Shouldly;

namespace AccessManagement.Application.UnitTests.Authorization;

[TestFixture]
public class DelegationValidityCapTests
{
    private ApplicationDbContext _db = null!;
    private IMediator _mediator = null!;
    private Permission _perm = null!;

    [SetUp]
    public async Task SetUp()
    {
        (_db, _) = AuthorizationTestContext.Create();
        await _db.Database.EnsureCreatedAsync();
        _perm = Permission.Create("PERSONNEL.READ", "Personnel", "Read");
        _db.Permissions.Add(_perm);
        await _db.SaveChangesAsync();
        _mediator = AuthorizationTestContext.CreateMediatorServices(_db).GetRequiredService<IMediator>();
    }

    [TearDown]
    public async Task TearDown()
    {
        await _db.Database.EnsureDeletedAsync();
        await _db.DisposeAsync();
    }

    [Test]
    public async Task Delegation_ValidTo_Is_Capped_To_Delegator_Grant()
    {
        var now = DateTimeOffset.UtcNow;
        _db.Grants.Add(Grant.CreateForUser(
            TestUsers.UserA, _perm.Id, SourceType.User, Guid.Empty, Effect.Allow,
            now.AddDays(-1), now.AddDays(1), SourcePriority.IndividualOverride));
        await _db.SaveChangesAsync();

        var id = await _mediator.Send(new CreateDelegationCommand(
            TestUsers.UserA, TestUsers.UserB, _perm.Id, now, now.AddYears(10)));

        var delegation = await _db.Delegations.FindAsync(id);
        delegation.ShouldNotBeNull();
        delegation!.ValidTo.ShouldNotBeNull();
        delegation.ValidTo!.Value.ShouldBe(now.AddDays(1), TimeSpan.FromSeconds(1));
        delegation.Delegable.ShouldBeFalse();
    }

    [Test]
    public async Task Parent_Delegation_Id_Is_Persisted_When_Parent_Is_Delegable()
    {
        var now = DateTimeOffset.UtcNow;
        _db.Grants.Add(Grant.CreateForUser(
            TestUsers.UserA, _perm.Id, SourceType.User, Guid.Empty, Effect.Allow,
            now.AddDays(-1), null, SourcePriority.IndividualOverride));
        _db.Grants.Add(Grant.CreateForUser(
            TestUsers.UserB, _perm.Id, SourceType.User, Guid.Empty, Effect.Allow,
            now.AddDays(-1), null, SourcePriority.IndividualOverride));
        await _db.SaveChangesAsync();

        var parentId = await _mediator.Send(new CreateDelegationCommand(
            TestUsers.UserA, TestUsers.UserB, _perm.Id, now, now.AddDays(3), Delegable: true));

        var childId = await _mediator.Send(new CreateDelegationCommand(
            TestUsers.UserB, TestUsers.UserC, _perm.Id, now, now.AddDays(10),
            ParentDelegationId: parentId));

        var child = await _db.Delegations.FindAsync(childId);
        child.ShouldNotBeNull();
        child!.ParentDelegationId.ShouldBe(parentId);
        child.ValidTo.ShouldNotBeNull();
        child.ValidTo!.Value.ShouldBe(now.AddDays(3), TimeSpan.FromSeconds(1));
    }
}
