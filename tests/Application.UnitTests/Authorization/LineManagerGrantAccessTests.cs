using AccessManagement.Application.Authorization.Commands.GrantAccess;
using AccessManagement.Application.UnitTests.TestSupport;
using AccessManagement.Domain.Authorization;
using AccessManagement.Domain.Authorization.Enums;
using AccessManagement.Domain.Authorization.Exceptions;
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
public class LineManagerGrantAccessTests
{
    private ApplicationDbContext _db = null!;
    private IMediator _mediator = null!;
    private Permission _shiftUpdate = null!;
    private static readonly DateTimeOffset T0 = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
    private static readonly Guid Reza = Guid.Parse("aaaaaaaa-0000-0000-0000-00000000aa01");
    private static readonly Guid Specialist = Guid.Parse("aaaaaaaa-0000-0000-0000-00000000aa02");

    [SetUp]
    public async Task SetUp()
    {
        (_db, _) = AuthorizationTestContext.Create();
        await _db.Database.EnsureCreatedAsync();
        _shiftUpdate = Permission.Create("SHIFT.UPDATE", "SHIFT", "UPDATE");
        _db.Permissions.Add(_shiftUpdate);
        _db.Permissions.Add(Permission.Create(CoreAccessPermissions.Grant, "ACCESS", "GRANT"));
        _db.Permissions.Add(Permission.Create(CoreAccessPermissions.AdministerAll, "ACCESS", "ADMINISTER_ALL"));
        await _db.SaveChangesAsync();
        _mediator = AuthorizationTestContext.CreateMediatorServices(_db, TestGuids.CompanyA).GetRequiredService<IMediator>();
    }

    [TearDown]
    public async Task TearDown()
    {
        await _db.Database.EnsureDeletedAsync();
        await _db.DisposeAsync();
    }

    [Test]
    public async Task Group10_Manager_Can_Grant_To_Subordinate_Position()
    {
        var managerPos = Position.Create(TestGuids.CompanyA, "MGR", "Manager");
        var childPos = Position.Create(TestGuids.CompanyA, "SPC", "Specialist", parentPositionId: managerPos.Id);
        _db.Positions.AddRange(managerPos, childPos);

        var reza = Personnel.Create("111", "Reza", "R", "P1", identityUserId: Reza);
        var spec = Personnel.Create("222", "Ali", "A", "P2", identityUserId: Specialist);
        _db.Personnel.AddRange(reza, spec);
        _db.PositionAssignments.Add(PositionAssignment.Create(reza.Id, managerPos.Id, T0));
        _db.PositionAssignments.Add(PositionAssignment.Create(spec.Id, childPos.Id, T0));

        _db.Grants.Add(Grant.CreateForUser(Reza, _shiftUpdate.Id, SourceType.Position, managerPos.Id, Effect.Allow, T0, null, SourcePriority.PositionOverride, scopeUnitId: TestGuids.CompanyA));
        var grantPerm = _db.Permissions.Local.First(p => p.Code == CoreAccessPermissions.Grant);
        _db.Grants.Add(Grant.CreateForUser(Reza, grantPerm.Id, SourceType.User, Reza, Effect.Allow, T0, null, SourcePriority.IndividualOverride, scopeUnitId: TestGuids.CompanyA));
        await _db.SaveChangesAsync();

        var id = await _mediator.Send(new GrantAccessCommand(
            Reza, TestGuids.CompanyA, AccessGrantTargetKind.Position, childPos.Id,
            _shiftUpdate.Id, TestGuids.CompanyA, T0, null));

        id.ShouldNotBe(Guid.Empty);
    }

    [Test]
    public async Task Group10_Cannot_Grant_To_Peer()
    {
        var managerPos = Position.Create(TestGuids.CompanyA, "MGR", "Manager");
        var peerPos = Position.Create(TestGuids.CompanyA, "PEER", "Peer");
        _db.Positions.AddRange(managerPos, peerPos);
        var reza = Personnel.Create("111", "Reza", "R", "P1", identityUserId: Reza);
        _db.Personnel.Add(reza);
        _db.PositionAssignments.Add(PositionAssignment.Create(reza.Id, managerPos.Id, T0));
        _db.Grants.Add(Grant.CreateForUser(Reza, _shiftUpdate.Id, SourceType.User, Reza, Effect.Allow, T0, null, SourcePriority.IndividualOverride, scopeUnitId: TestGuids.CompanyA));
        var grantPerm = _db.Permissions.Local.First(p => p.Code == CoreAccessPermissions.Grant);
        _db.Grants.Add(Grant.CreateForUser(Reza, grantPerm.Id, SourceType.User, Reza, Effect.Allow, T0, null, SourcePriority.IndividualOverride, scopeUnitId: TestGuids.CompanyA));
        await _db.SaveChangesAsync();

        await Should.ThrowAsync<AuthorizationDomainException>(() =>
            _mediator.Send(new GrantAccessCommand(
                Reza, TestGuids.CompanyA, AccessGrantTargetKind.Position, peerPos.Id,
                _shiftUpdate.Id, TestGuids.CompanyA, T0, null)));
    }

    [Test]
    public async Task Group10_System_User_Bypasses_Subset()
    {
        var pos = Position.Create(TestGuids.CompanyA, "ANY", "Any");
        _db.Positions.Add(pos);
        var admin = Personnel.Create("000", "Admin", "A", "ADM", identityUserId: Reza, isSystemUser: true);
        _db.Personnel.Add(admin);
        await _db.SaveChangesAsync();

        var id = await _mediator.Send(new GrantAccessCommand(
            Reza, TestGuids.CompanyA, AccessGrantTargetKind.Position, pos.Id,
            _shiftUpdate.Id, TestGuids.CompanyA, T0, null));
        id.ShouldNotBe(Guid.Empty);
    }
}
