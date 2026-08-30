using AccessManagement.Application.Authorization.Commands.RevokeAuthorizationRoleFromPosition;
using AccessManagement.Application.Authorization.Commands.RevokeAuthorizationRoleFromUser;
using AccessManagement.Application.Authorization.Evaluation;
using AccessManagement.Application.UnitTests.TestSupport;
using AccessManagement.Domain.Authorization;
using AccessManagement.Domain.Authorization.Enums;
using AccessManagement.Domain.Authorization.Evaluation;
using AccessManagement.Domain.Authorization.ValueObjects;
using AccessManagement.Domain.Organization;
using AccessManagement.Infrastructure.Data;
using AccessManagement.Tests.TestSupport;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Shouldly;

namespace AccessManagement.Application.UnitTests.Authorization;

[TestFixture]
public class RevokeRoleConsistencyTests
{
    private ApplicationDbContext _db = null!;
    private AccessEvaluator _evaluator = null!;
    private IMediator _mediator = null!;
    private Permission _perm = null!;
    private static readonly DateTimeOffset T0 = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    [SetUp]
    public async Task SetUp()
    {
        (_db, _evaluator) = AuthorizationTestContext.Create();
        await _db.Database.EnsureCreatedAsync();
        _perm = Permission.Create("RESOURCE.READ", "RESOURCE", "READ");
        _db.Permissions.Add(_perm);
        _db.Personnel.Add(Personnel.Create("100", "Ada", "Admin", "A1", identityUserId: TestUsers.UserA, isSystemUser: true));
        await _db.SaveChangesAsync();
        _mediator = AuthorizationTestContext.CreateMediatorServices(_db, userId: TestUsers.UserA)
            .GetRequiredService<IMediator>();
    }

    [TearDown]
    public async Task TearDown()
    {
        await _db.Database.EnsureDeletedAsync();
        await _db.DisposeAsync();
    }

    [Test]
    public async Task RevokeAuthorizationRoleFromPosition_Deactivates_Not_Deletes()
    {
        var position = Position.Create(TestGuids.CompanyA, "POS", "Position");
        var role = Role.Create("READER", "Reader");
        _db.Positions.Add(position);
        _db.AuthorizationRoles.Add(role);
        var grant = Grant.Create(
            SubjectType.Position,
            position.Id,
            _perm.Id,
            SourceType.Role,
            role.Id,
            Effect.Allow,
            T0,
            null,
            SourcePriority.RoleOrRoleGroup,
            scopeUnitId: TestGuids.CompanyA);
        _db.Grants.Add(grant);
        await _db.SaveChangesAsync();

        grant.ValidTo.ShouldBeNull();

        await _mediator.Send(new RevokeAuthorizationRoleFromPositionCommand(position.Id, role.Id));

        var persisted = await _db.Grants.FindAsync(grant.Id);
        persisted.ShouldNotBeNull();
        persisted.ValidTo.ShouldNotBeNull();
        persisted.ValidTo.Value.ShouldBeLessThanOrEqualTo(DateTimeOffset.UtcNow);
    }

    [Test]
    public async Task RevokeAuthorizationRoleFromUser_Deactivates_Not_Deletes()
    {
        var role = Role.Create("READER", "Reader");
        _db.AuthorizationRoles.Add(role);
        var grant = Grant.CreateForUser(
            TestUsers.UserB,
            _perm.Id,
            SourceType.Role,
            role.Id,
            Effect.Allow,
            T0,
            null,
            SourcePriority.RoleOrRoleGroup,
            scopeUnitId: TestGuids.CompanyA);
        _db.Grants.Add(grant);
        await _db.SaveChangesAsync();

        grant.ValidTo.ShouldBeNull();

        await _mediator.Send(new RevokeAuthorizationRoleFromUserCommand(TestUsers.UserB, role.Id));

        var persisted = await _db.Grants.FindAsync(grant.Id);
        persisted.ShouldNotBeNull();
        persisted.ValidTo.ShouldNotBeNull();
        persisted.ValidTo.Value.ShouldBeLessThanOrEqualTo(DateTimeOffset.UtcNow);
    }

    [Test]
    public async Task Deactivated_Role_Grant_Does_Not_Appear_In_Active_Evaluation()
    {
        var role = Role.Create("READER", "Reader");
        _db.AuthorizationRoles.Add(role);
        var grant = Grant.CreateForUser(
            TestUsers.UserB,
            _perm.Id,
            SourceType.Role,
            role.Id,
            Effect.Allow,
            T0,
            null,
            SourcePriority.RoleOrRoleGroup,
            scopeUnitId: TestGuids.CompanyA);
        _db.Grants.Add(grant);
        await _db.SaveChangesAsync();

        var before = await _evaluator.EvaluateAsync(AccessRequest.ForUser(
            TestUsers.UserB,
            "RESOURCE.READ",
            resourceScopeUnitId: TestGuids.CompanyA,
            when: T0));
        before.Allowed.ShouldBeTrue();

        await _mediator.Send(new RevokeAuthorizationRoleFromUserCommand(TestUsers.UserB, role.Id));
        await _db.Entry(grant).ReloadAsync();

        var after = await _evaluator.EvaluateAsync(AccessRequest.ForUser(
            TestUsers.UserB,
            "RESOURCE.READ",
            resourceScopeUnitId: TestGuids.CompanyA,
            when: grant.ValidTo!.Value.AddSeconds(1)));
        after.Allowed.ShouldBeFalse();
        after.Effect.ShouldBe(Effect.Deny);
    }
}
