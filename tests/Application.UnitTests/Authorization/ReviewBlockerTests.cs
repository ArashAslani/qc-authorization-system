using AccessManagement.Application.Authorization.Commands.CreateDelegation;
using AccessManagement.Application.Authorization.Commands.CreatePermission;
using AccessManagement.Application.Authorization.Commands.EvaluateAccessBatch;
using AccessManagement.Application.Authorization.Commands.RevokeDelegation;
using AccessManagement.Application.Authorization.Queries.EvaluateAccess;
using AccessManagement.Application.Organization.Commands.AssignPersonnelToPosition;
using AccessManagement.Application.Organization.Commands.CreatePosition;
using AccessManagement.Application.Organization.Queries.GetPersonnel;
using AccessManagement.Domain.Organization.Exceptions;
using AccessManagement.Application.Common.Exceptions;
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
public class ReviewBlockerTests
{
    private ApplicationDbContext _db = null!;
    private static readonly DateTimeOffset T0 = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
    private static readonly Guid AdminUser = Guid.Parse("bbbbbbbb-0000-0000-0000-000000000001");
    private static readonly Guid WorkerUser = Guid.Parse("bbbbbbbb-0000-0000-0000-000000000002");
    private static readonly Guid BossUser = Guid.Parse("bbbbbbbb-0000-0000-0000-000000000003");
    private static readonly Guid StrangerUser = Guid.Parse("bbbbbbbb-0000-0000-0000-000000000004");

    [SetUp]
    public async Task SetUp()
    {
        (_db, _) = AuthorizationTestContext.Create();
        await _db.Database.EnsureCreatedAsync();
        AuthorizationTestContext.SeedCompany(_db, TestGuids.CompanyB, "B");

        var admin = Personnel.Create("100", "Ada", "Admin", "A1", identityUserId: AdminUser, isSystemUser: true);
        var worker = Personnel.Create("200", "Will", "Worker", "W1", identityUserId: WorkerUser);
        var boss = Personnel.Create("300", "Bea", "Boss", "B1", identityUserId: BossUser);
        var other = Personnel.Create("400", "Oth", "Other", "O1", identityUserId: StrangerUser);
        _db.Personnel.AddRange(admin, worker, boss, other);

        var bossPos = Position.Create(TestGuids.CompanyA, "BOS", "Boss");
        var workerPos = Position.Create(TestGuids.CompanyA, "WKR", "Worker", parentPositionId: bossPos.Id);
        var otherPos = Position.Create(TestGuids.CompanyB, "B-W", "B Worker");
        _db.Positions.AddRange(workerPos, bossPos, otherPos);
        _db.PositionAssignments.Add(PositionAssignment.Create(worker.Id, workerPos.Id, T0));
        _db.PositionAssignments.Add(PositionAssignment.Create(boss.Id, bossPos.Id, T0));
        _db.PositionAssignments.Add(PositionAssignment.Create(other.Id, otherPos.Id, T0));
        await _db.SaveChangesAsync();
    }

    [TearDown]
    public async Task TearDown()
    {
        await _db.Database.EnsureDeletedAsync();
        await _db.DisposeAsync();
    }

    [Test]
    public async Task B1_NonAdmin_Cannot_Create_Permission()
    {
        var mediator = MediatorFor(WorkerUser);
        await Should.ThrowAsync<ForbiddenAccessException>(() =>
            mediator.Send(new CreatePermissionCommand("X", "X", "Y", "Y")));
    }

    [Test]
    public async Task B1_SystemUser_Can_Create_Permission()
    {
        var mediator = MediatorFor(AdminUser);
        var id = await mediator.Send(new CreatePermissionCommand("X", "X", "Y", "Y"));
        id.ShouldNotBe(Guid.Empty);
    }

    [Test]
    public async Task B3_Spoofed_Position_Is_Forbidden()
    {
        var bossPosId = _db.Positions.Single(p => p.Code == "BOS").Id;
        var mediator = MediatorFor(WorkerUser);
        await Should.ThrowAsync<ForbiddenAccessException>(() =>
            mediator.Send(new EvaluateAccessQuery(WorkerUser, "RESOURCE.READ", bossPosId, TestGuids.CompanyA)));
    }

    [Test]
    public async Task B4_Cannot_Create_Delegation_As_Someone_Else()
    {
        var perm = Permission.Create("TASK.DO", "TASK", "DO");
        _db.Permissions.Add(perm);
        _db.Grants.Add(Grant.CreateForUser(
            BossUser, perm.Id, SourceType.User, BossUser, Effect.Allow, T0, null,
            SourcePriority.IndividualOverride, scopeUnitId: TestGuids.CompanyA));
        await _db.SaveChangesAsync();

        var mediator = MediatorFor(WorkerUser);
        await Should.ThrowAsync<ForbiddenAccessException>(() =>
            mediator.Send(new CreateDelegationCommand(
                BossUser, WorkerUser, perm.Id, T0, null, DelegatorCompanyUnitId: TestGuids.CompanyA)));
    }

    [Test]
    public async Task B6_Stranger_Cannot_Revoke_Others_Delegation()
    {
        var perm = Permission.Create("TASK.DO", "TASK", "DO");
        _db.Permissions.Add(perm);
        _db.Grants.Add(Grant.CreateForUser(
            BossUser, perm.Id, SourceType.User, BossUser, Effect.Allow, T0, null,
            SourcePriority.IndividualOverride, scopeUnitId: TestGuids.CompanyA));
        await _db.SaveChangesAsync();

        var ownerMediator = MediatorFor(BossUser);
        var id = await ownerMediator.Send(new CreateDelegationCommand(
            BossUser, WorkerUser, perm.Id, T0, null,
            ScopeUnitId: TestGuids.CompanyA,
            DelegatorCompanyUnitId: TestGuids.CompanyA));

        var stranger = MediatorFor(StrangerUser, TestGuids.CompanyB);
        await Should.ThrowAsync<ForbiddenAccessException>(() =>
            stranger.Send(new RevokeDelegationCommand(id, StrangerUser)));

        _db.Delegations.Single(d => d.Id == id).IsRevoked.ShouldBeFalse();
    }

    [Test]
    public async Task B5_NonAdmin_Sees_Only_Session_Company_Personnel()
    {
        var mediator = MediatorFor(WorkerUser);
        var list = await mediator.Send(new GetPersonnelQuery());
        list.Items.Select(p => p.IdentityUserId).ShouldContain(WorkerUser);
        list.Items.Select(p => p.IdentityUserId).ShouldNotContain(BossUser);
        list.Items.Select(p => p.IdentityUserId).ShouldNotContain(StrangerUser);
    }

    [Test]
    public async Task H3_CreatePosition_Rejects_Non_Company_Unit()
    {
        var holding = OrganizationalUnit.Create(OrganizationalUnitTypes.Holding, "H");
        _db.OrganizationalUnits.Add(holding);
        await _db.SaveChangesAsync();

        var mediator = MediatorFor(AdminUser);
        await Should.ThrowAsync<OrganizationDomainException>(() =>
            mediator.Send(new CreatePositionCommand(holding.Id, "X", "X", null, null)));
    }

    [Test]
    public async Task Group6_Individual_Persists_After_Reassignment()
    {
        var perm = Permission.Create("RESOURCE.READ", "RESOURCE", "READ");
        _db.Permissions.Add(perm);
        _db.Grants.Add(Grant.CreateForUser(
            WorkerUser, perm.Id, SourceType.User, WorkerUser, Effect.Allow, T0, null,
            SourcePriority.IndividualOverride, scopeUnitId: TestGuids.CompanyA));
        var newPos = Position.Create(TestGuids.CompanyA, "NEW", "New");
        _db.Positions.Add(newPos);
        await _db.SaveChangesAsync();

        var workerPersonnelId = _db.Personnel.Single(p => p.IdentityUserId == WorkerUser).Id;
        var oldPosId = _db.Positions.Single(p => p.Code == "WKR").Id;
        var admin = MediatorFor(AdminUser);
        await admin.Send(new AssignPersonnelToPositionCommand(workerPersonnelId, newPos.Id, T0.AddHours(1)));

        var worker = MediatorFor(WorkerUser);
        var after = await worker.Send(new EvaluateAccessQuery(
            WorkerUser, "RESOURCE.READ", newPos.Id, TestGuids.CompanyA));
        after.Allowed.ShouldBeTrue();

        await Should.ThrowAsync<ForbiddenAccessException>(() =>
            worker.Send(new EvaluateAccessQuery(
                WorkerUser, "RESOURCE.READ", oldPosId, TestGuids.CompanyA)));
    }

    [Test]
    public async Task EvaluateBatch_Continues_After_Spoofed_Row()
    {
        var perm = Permission.Create("RESOURCE.READ", "RESOURCE", "READ");
        _db.Permissions.Add(perm);
        _db.Grants.Add(Grant.CreateForUser(
            WorkerUser, perm.Id, SourceType.User, WorkerUser, Effect.Allow, T0, null,
            SourcePriority.IndividualOverride, scopeUnitId: TestGuids.CompanyA));
        await _db.SaveChangesAsync();

        var workerPosId = _db.Positions.Single(p => p.Code == "WKR").Id;
        var bossPosId = _db.Positions.Single(p => p.Code == "BOS").Id;
        var result = await MediatorFor(AdminUser).Send(new EvaluateAccessBatchCommand(
        [
            new EvaluateAccessBatchItem(WorkerUser, "RESOURCE.READ", workerPosId, TestGuids.CompanyA),
            new EvaluateAccessBatchItem(WorkerUser, "RESOURCE.READ", bossPosId, TestGuids.CompanyA),
        ]));

        result.Rows.Count.ShouldBe(2);
        result.Rows[0].Succeeded.ShouldBeTrue();
        result.Rows[0].Allowed.ShouldBeTrue();
        result.Rows[1].Succeeded.ShouldBeFalse();
        result.Rows[1].Error.ShouldNotBeNull();
    }

    private IMediator MediatorFor(Guid userId, Guid? companyId = null) =>
        AuthorizationTestContext.CreateMediatorServices(_db, companyId ?? TestGuids.CompanyA, userId)
            .GetRequiredService<IMediator>();
}
