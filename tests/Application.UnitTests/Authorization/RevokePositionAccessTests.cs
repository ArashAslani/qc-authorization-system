using AccessManagement.Application.Authorization.Commands.RevokePositionAccess;
using AccessManagement.Application.UnitTests.TestSupport;
using AccessManagement.Domain.Authorization;
using AccessManagement.Domain.Authorization.Enums;
using AccessManagement.Domain.Authorization.ValueObjects;
using AccessManagement.Domain.Organization;
using AccessManagement.Infrastructure.Data;
using AccessManagement.Tests.TestSupport;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Shouldly;

namespace AccessManagement.Application.UnitTests.Authorization;

[TestFixture]
public class RevokePositionAccessTests
{
    private ApplicationDbContext _db = null!;
    private IMediator _mediator = null!;
    private Permission _perm = null!;
    private static readonly DateTimeOffset T0 = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    [SetUp]
    public async Task SetUp()
    {
        (_db, _) = AuthorizationTestContext.Create();
        await _db.Database.EnsureCreatedAsync();
        _perm = Permission.Create("RESOURCE.READ", "RESOURCE", "READ");
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
    public async Task Revoke_Deactivates_Position_And_Descendants_Not_Ancestors()
    {
        var deputy = Position.Create(TestGuids.CompanyA, "DEP", "Deputy");
        var manager = Position.Create(TestGuids.CompanyA, "MGR", "Manager", parentPositionId: deputy.Id);
        var specialist = Position.Create(TestGuids.CompanyA, "SPC", "Specialist", parentPositionId: manager.Id);
        _db.Positions.AddRange(deputy, manager, specialist);

        var ancestorGrant = Grant.Create(SubjectType.Position, deputy.Id, _perm.Id, SourceType.Position, deputy.Id, Effect.Allow, T0, null, SourcePriority.PositionOverride, scopeUnitId: TestGuids.CompanyA);
        var managerGrant = Grant.Create(SubjectType.Position, manager.Id, _perm.Id, SourceType.Position, manager.Id, Effect.Allow, T0, null, SourcePriority.PositionOverride, scopeUnitId: TestGuids.CompanyA);
        var childGrant = Grant.Create(SubjectType.Position, specialist.Id, _perm.Id, SourceType.Position, specialist.Id, Effect.Allow, T0, null, SourcePriority.PositionOverride, scopeUnitId: TestGuids.CompanyA);
        _db.Grants.AddRange(ancestorGrant, managerGrant, childGrant);
        await _db.SaveChangesAsync();

        await _mediator.Send(new RevokePositionAccessCommand(manager.Id, _perm.Id, TestGuids.CompanyA, TestUsers.UserA));

        (await _db.Grants.FindAsync(ancestorGrant.Id))!.ValidTo.ShouldBeNull();
        (await _db.Grants.FindAsync(managerGrant.Id))!.ValidTo.ShouldNotBeNull();
        (await _db.Grants.FindAsync(childGrant.Id))!.ValidTo.ShouldNotBeNull();
    }
}
