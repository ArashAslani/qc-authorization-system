using AccessManagement.Application.Authorization.Commands.GrantAccess;
using AccessManagement.Application.Authorization.Commands.RevokeAccess;
using AccessManagement.Application.Authorization.Queries.GetGrantTargets;
using AccessManagement.Application.UnitTests.TestSupport;
using AccessManagement.Domain.Authorization;
using AccessManagement.Domain.Authorization.Enums;
using AccessManagement.Domain.Authorization.Exceptions;
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
public class LineManagerGrantAccessTests
{
    private ApplicationDbContext _db = null!;
    private IMediator _mediator = null!;
    private Permission _shiftUpdate = null!;
    private Permission _controlPlan = null!;
    private Permission _grantPerm = null!;
    private Permission _revokePerm = null!;
    private Permission _adminAll = null!;
    private Position _managerPos = null!;
    private Position _childPos = null!;
    private Position _peerPos = null!;
    private Position _bossPos = null!;
    private OrganizationalUnit _holding = null!;
    private OrganizationalUnit _workstation = null!;
    private static readonly DateTimeOffset T0 = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
    private static readonly Guid Reza = Guid.Parse("aaaaaaaa-0000-0000-0000-00000000aa01");
    private static readonly Guid Specialist = Guid.Parse("aaaaaaaa-0000-0000-0000-00000000aa02");
    private static readonly Guid PeerUser = Guid.Parse("aaaaaaaa-0000-0000-0000-00000000aa03");
    private static readonly Guid BossUser = Guid.Parse("aaaaaaaa-0000-0000-0000-00000000aa04");
    private static readonly Guid AdminUser = Guid.Parse("aaaaaaaa-0000-0000-0000-00000000aa05");

    [SetUp]
    public async Task SetUp()
    {
        (_db, _) = AuthorizationTestContext.Create();
        await _db.Database.EnsureCreatedAsync();

        _holding = OrganizationalUnit.Create(OrganizationalUnitTypes.Holding, "Holding");
        _db.OrganizationalUnits.Add(_holding);
        _workstation = OrganizationalUnit.Create(OrganizationalUnitTypes.Workstation, "WS-A", TestGuids.CompanyA);
        _db.OrganizationalUnits.Add(_workstation);

        AuthorizationTestContext.SeedCompany(_db, TestGuids.CompanyB, "B");

        _shiftUpdate = Permission.Create("SHIFT.UPDATE", "SHIFT", "UPDATE");
        _controlPlan = Permission.Create("CONTROLPLAN.APPROVE", "CONTROLPLAN", "APPROVE");
        _grantPerm = Permission.Create(CoreAccessPermissions.Grant, "ACCESS", "GRANT");
        _revokePerm = Permission.Create(CoreAccessPermissions.Revoke, "ACCESS", "REVOKE");
        _adminAll = Permission.Create(CoreAccessPermissions.AdministerAll, "ACCESS", "ADMINISTER_ALL");
        _db.Permissions.AddRange(_shiftUpdate, _controlPlan, _grantPerm, _revokePerm, _adminAll);

        _bossPos = Position.Create(TestGuids.CompanyA, "BOSS", "Director");
        _managerPos = Position.Create(TestGuids.CompanyA, "MGR", "Manager", parentPositionId: _bossPos.Id);
        _childPos = Position.Create(TestGuids.CompanyA, "SPC", "Specialist", parentPositionId: _managerPos.Id);
        _peerPos = Position.Create(TestGuids.CompanyA, "PEER", "Peer", parentPositionId: _bossPos.Id);
        var otherCompanyPos = Position.Create(TestGuids.CompanyB, "B-MGR", "B Manager");
        _db.Positions.AddRange(_bossPos, _managerPos, _childPos, _peerPos, otherCompanyPos);

        var reza = Personnel.Create("111", "Reza", "R", "P1", identityUserId: Reza);
        var spec = Personnel.Create("222", "Ali", "A", "P2", identityUserId: Specialist);
        var peer = Personnel.Create("333", "Peer", "P", "P3", identityUserId: PeerUser);
        var boss = Personnel.Create("444", "Boss", "B", "P4", identityUserId: BossUser);
        _db.Personnel.AddRange(reza, spec, peer, boss);
        _db.PositionAssignments.Add(PositionAssignment.Create(reza.Id, _managerPos.Id, T0));
        _db.PositionAssignments.Add(PositionAssignment.Create(spec.Id, _childPos.Id, T0));
        _db.PositionAssignments.Add(PositionAssignment.Create(peer.Id, _peerPos.Id, T0));
        _db.PositionAssignments.Add(PositionAssignment.Create(boss.Id, _bossPos.Id, T0));

        GrantOnPosition(_managerPos.Id, _shiftUpdate.Id);
        GrantOnPosition(_managerPos.Id, _grantPerm.Id);
        GrantOnPosition(_managerPos.Id, _revokePerm.Id);

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
        var id = await _mediator.Send(GrantCmd(AccessGrantTargetKind.Position, _childPos.Id, _shiftUpdate.Id, TestGuids.CompanyA));
        id.ShouldNotBe(Guid.Empty);
    }

    [Test]
    public async Task Group10_Manager_Can_Grant_To_Subordinate_User()
    {
        var id = await _mediator.Send(GrantCmd(AccessGrantTargetKind.User, Specialist, _shiftUpdate.Id, TestGuids.CompanyA));
        id.ShouldNotBe(Guid.Empty);
    }

    [Test]
    public async Task Group10_Cannot_Grant_To_Peer()
    {
        await Should.ThrowAsync<AuthorizationDomainException>(() =>
            _mediator.Send(GrantCmd(AccessGrantTargetKind.Position, _peerPos.Id, _shiftUpdate.Id, TestGuids.CompanyA)));
    }

    [Test]
    public async Task Group10_Cannot_Grant_To_Boss()
    {
        await Should.ThrowAsync<AuthorizationDomainException>(() =>
            _mediator.Send(GrantCmd(AccessGrantTargetKind.Position, _bossPos.Id, _shiftUpdate.Id, TestGuids.CompanyA)));
    }

    [Test]
    public async Task Group10_Cannot_Grant_To_Other_Company()
    {
        var otherPos = await _db.Positions.AsNoTracking().SingleAsync(p => p.Code == "B-MGR");
        await Should.ThrowAsync<AuthorizationDomainException>(() =>
            _mediator.Send(GrantCmd(AccessGrantTargetKind.Position, otherPos.Id, _shiftUpdate.Id, TestGuids.CompanyA)));
    }

    [Test]
    public async Task Group10_Cannot_Grant_Missing_Permission()
    {
        await Should.ThrowAsync<AuthorizationDomainException>(() =>
            _mediator.Send(GrantCmd(AccessGrantTargetKind.Position, _childPos.Id, _controlPlan.Id, TestGuids.CompanyA)));
    }

    [Test]
    public async Task Group10_Cannot_Grant_Wider_Holding_Scope()
    {
        await Should.ThrowAsync<AuthorizationDomainException>(() =>
            _mediator.Send(GrantCmd(AccessGrantTargetKind.Position, _childPos.Id, _shiftUpdate.Id, _holding.Id)));
    }

    [Test]
    public async Task Group10_Can_Narrow_Scope_To_Workstation()
    {
        var id = await _mediator.Send(GrantCmd(AccessGrantTargetKind.Position, _childPos.Id, _shiftUpdate.Id, _workstation.Id));
        id.ShouldNotBe(Guid.Empty);
    }

    [Test]
    public async Task Group10_System_User_Bypasses_Subset()
    {
        var admin = Personnel.Create("000", "Admin", "A", "ADM", identityUserId: AdminUser, isSystemUser: true);
        _db.Personnel.Add(admin);
        await _db.SaveChangesAsync();

        var id = await _mediator.Send(new GrantAccessCommand(
            AdminUser, TestGuids.CompanyA, AccessGrantTargetKind.Position, _peerPos.Id,
            _shiftUpdate.Id, TestGuids.CompanyA, T0, null));
        id.ShouldNotBe(Guid.Empty);
    }

    [Test]
    public async Task Group10_AdministerAll_Without_SystemUser_Bypasses_Subset()
    {
        var admin = Personnel.Create("001", "Power", "A", "ADM2", identityUserId: AdminUser);
        _db.Personnel.Add(admin);
        _db.Grants.Add(Grant.CreateForUser(
            AdminUser, _adminAll.Id, SourceType.User, AdminUser, Effect.Allow, T0, null,
            SourcePriority.IndividualOverride, scopeUnitId: TestGuids.CompanyA));
        await _db.SaveChangesAsync();

        var id = await _mediator.Send(new GrantAccessCommand(
            AdminUser, TestGuids.CompanyA, AccessGrantTargetKind.Position, _peerPos.Id,
            _shiftUpdate.Id, TestGuids.CompanyA, T0, null));
        id.ShouldNotBe(Guid.Empty);
    }

    [Test]
    public async Task Group10_Access_Targets_Hides_Sibling()
    {
        var targets = await _mediator.Send(new GetGrantTargetsQuery(Reza, TestGuids.CompanyA));
        targets.Positions.ShouldContain(p => p.PositionId == _childPos.Id);
        targets.Positions.ShouldNotContain(p => p.PositionId == _peerPos.Id);
        targets.Users.ShouldContain(u => u.UserId == Specialist);
    }

    [Test]
    public async Task Group10_Revoke_Subordinate_Succeeds()
    {
        await _mediator.Send(GrantCmd(AccessGrantTargetKind.Position, _childPos.Id, _shiftUpdate.Id, TestGuids.CompanyA));
        await _mediator.Send(new RevokeAccessCommand(
            Reza, TestGuids.CompanyA, AccessGrantTargetKind.Position, _childPos.Id, _shiftUpdate.Id, TestGuids.CompanyA));

        var grant = await _db.Grants.SingleAsync(g => g.SubjectId == _childPos.Id && g.PermissionId == _shiftUpdate.Id);
        grant.ValidTo.ShouldNotBeNull();
    }

    [Test]
    public async Task Group10_Revoke_Outside_Subtree_Rejected()
    {
        await Should.ThrowAsync<AuthorizationDomainException>(() =>
            _mediator.Send(new RevokeAccessCommand(
                Reza, TestGuids.CompanyA, AccessGrantTargetKind.Position, _peerPos.Id, _shiftUpdate.Id, TestGuids.CompanyA)));
    }

    private void GrantOnPosition(Guid positionId, Guid permissionId) =>
        _db.Grants.Add(Grant.Create(
            SubjectType.Position,
            positionId,
            permissionId,
            SourceType.Position,
            positionId,
            Effect.Allow,
            T0,
            null,
            SourcePriority.PositionOverride,
            scopeUnitId: TestGuids.CompanyA));

    private GrantAccessCommand GrantCmd(AccessGrantTargetKind kind, Guid targetId, Guid permissionId, Guid scopeUnitId) =>
        new(Reza, TestGuids.CompanyA, kind, targetId, permissionId, scopeUnitId, T0, null);
}
