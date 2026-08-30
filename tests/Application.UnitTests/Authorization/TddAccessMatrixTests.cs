using AccessManagement.Application.UnitTests.TestSupport;
using AccessManagement.Domain.Authorization;
using AccessManagement.Domain.Authorization.Enums;
using AccessManagement.Domain.Authorization.Evaluation;
using AccessManagement.Domain.Authorization.ValueObjects;
using AccessManagement.Domain.Organization;
using AccessManagement.Infrastructure.Data;
using AccessManagement.Tests.TestSupport;
using NUnit.Framework;
using Shouldly;

namespace AccessManagement.Application.UnitTests.Authorization;

[TestFixture]
public class TddAccessMatrixTests
{
    private ApplicationDbContext _db = null!;
    private AccessManagement.Application.Authorization.Evaluation.AccessEvaluator _evaluator = null!;
    private Permission _perm = null!;
    private static readonly DateTimeOffset T0 = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
    private static readonly Guid UserA = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000001");

    [SetUp]
    public async Task SetUp()
    {
        (_db, _evaluator) = AuthorizationTestContext.Create();
        await _db.Database.EnsureCreatedAsync();
        AuthorizationTestContext.SeedCompany(_db, TestGuids.CompanyA);

        _perm = Permission.Create("RESOURCE.READ", "RESOURCE", "READ");
        _db.Permissions.Add(_perm);
        await _db.SaveChangesAsync();
    }

    [TearDown]
    public async Task TearDown()
    {
        await _db.Database.EnsureDeletedAsync();
        await _db.DisposeAsync();
    }

    [Test]
    public async Task Group1_User_Direct_Allow()
    {
        _db.Grants.Add(Grant.CreateForUser(UserA, _perm.Id, SourceType.User, UserA, Effect.Allow, T0, null, SourcePriority.IndividualOverride, scopeUnitId: TestGuids.CompanyA));
        await _db.SaveChangesAsync();

        var d = await _evaluator.EvaluateAsync(AccessRequest.ForUser(UserA, "RESOURCE.READ", resourceScopeUnitId: TestGuids.CompanyA, when: T0));
        d.Allowed.ShouldBeTrue();
    }

    [Test]
    public async Task Group1_User_Direct_Deny()
    {
        _db.Grants.Add(Grant.CreateForUser(UserA, _perm.Id, SourceType.User, UserA, Effect.Deny, T0, null, SourcePriority.IndividualOverride, scopeUnitId: TestGuids.CompanyA));
        await _db.SaveChangesAsync();

        var d = await _evaluator.EvaluateAsync(AccessRequest.ForUser(UserA, "RESOURCE.READ", resourceScopeUnitId: TestGuids.CompanyA, when: T0));
        d.Allowed.ShouldBeFalse();
        d.Reason.ShouldStartWith(AccessDecisionReasons.Overridden);
    }

    [Test]
    public async Task Group2_Individual_Deny_Beats_Role_Allow()
    {
        var position = Position.Create(TestGuids.CompanyA, "P1", "P1");
        _db.Positions.Add(position);
        _db.Grants.Add(Grant.Create(SubjectType.Position, position.Id, _perm.Id, SourceType.Role, Guid.NewGuid(), Effect.Allow, T0, null, SourcePriority.RoleOrRoleGroup, scopeUnitId: TestGuids.CompanyA));
        _db.Grants.Add(Grant.CreateForUser(UserA, _perm.Id, SourceType.User, UserA, Effect.Deny, T0, null, SourcePriority.IndividualOverride, scopeUnitId: TestGuids.CompanyA));
        await _db.SaveChangesAsync();

        var d = await _evaluator.EvaluateAsync(AccessRequest.ForUser(UserA, "RESOURCE.READ", position.Id, TestGuids.CompanyA, T0));
        d.Allowed.ShouldBeFalse();
    }

    [Test]
    public async Task Group3_Expired_Grant_Denies()
    {
        _db.Grants.Add(Grant.CreateForUser(UserA, _perm.Id, SourceType.User, UserA, Effect.Allow, T0.AddDays(-10), T0.AddDays(-1), SourcePriority.IndividualOverride, scopeUnitId: TestGuids.CompanyA));
        await _db.SaveChangesAsync();

        var d = await _evaluator.EvaluateAsync(AccessRequest.ForUser(UserA, "RESOURCE.READ", resourceScopeUnitId: TestGuids.CompanyA, when: T0));
        d.Allowed.ShouldBeFalse();
        d.Reason.ShouldBe(AccessDecisionReasons.Expired);
    }

    [Test]
    public async Task Group4_Subtree_Scope_Matches_Descendant()
    {
        var company = await _db.OrganizationalUnits.FindAsync(TestGuids.CompanyA);
        var station = OrganizationalUnit.Create(OrganizationalUnitTypes.Workstation, "WS-1", company!.Id);
        _db.OrganizationalUnits.Add(station);
        _db.Grants.Add(Grant.CreateForUser(UserA, _perm.Id, SourceType.User, UserA, Effect.Allow, T0, null, SourcePriority.IndividualOverride, scopeUnitId: company.Id));
        await _db.SaveChangesAsync();

        var d = await _evaluator.EvaluateAsync(AccessRequest.ForUser(UserA, "RESOURCE.READ", resourceScopeUnitId: station.Id, when: T0));
        d.Allowed.ShouldBeTrue();
    }

    [Test]
    public async Task Group4_Child_Scope_Does_Not_Match_Parent_Resource()
    {
        var company = await _db.OrganizationalUnits.FindAsync(TestGuids.CompanyA);
        var station = OrganizationalUnit.Create(OrganizationalUnitTypes.Workstation, "WS-1", company!.Id);
        _db.OrganizationalUnits.Add(station);
        _db.Grants.Add(Grant.CreateForUser(UserA, _perm.Id, SourceType.User, UserA, Effect.Allow, T0, null, SourcePriority.IndividualOverride, scopeUnitId: station.Id));
        await _db.SaveChangesAsync();

        var d = await _evaluator.EvaluateAsync(AccessRequest.ForUser(UserA, "RESOURCE.READ", resourceScopeUnitId: company.Id, when: T0));
        d.Allowed.ShouldBeFalse();
        d.Reason.ShouldBe(AccessDecisionReasons.OutOfScope);
    }

    [Test]
    public async Task Group5_Allow_On_Specialist_Propagates_To_Manager()
    {
        var deputy = Position.Create(TestGuids.CompanyA, "DEP", "Deputy");
        var manager = Position.Create(TestGuids.CompanyA, "MGR", "Manager", parentPositionId: deputy.Id);
        var supervisor = Position.Create(TestGuids.CompanyA, "SUP", "Supervisor", parentPositionId: manager.Id);
        var specialist = Position.Create(TestGuids.CompanyA, "SPC", "Specialist", parentPositionId: supervisor.Id);
        _db.Positions.AddRange(deputy, manager, supervisor, specialist);
        _db.Grants.Add(Grant.Create(SubjectType.Position, specialist.Id, _perm.Id, SourceType.Position, specialist.Id, Effect.Allow, T0, null, SourcePriority.PositionOverride, scopeUnitId: TestGuids.CompanyA));
        await _db.SaveChangesAsync();

        var asDeputy = await _evaluator.EvaluateAsync(AccessRequest.ForUser(UserA, "RESOURCE.READ", deputy.Id, TestGuids.CompanyA, T0));
        asDeputy.Allowed.ShouldBeTrue();
    }

    [Test]
    public async Task Group5_Deny_On_Specialist_Does_Not_Propagate_To_Manager()
    {
        var manager = Position.Create(TestGuids.CompanyA, "MGR", "Manager");
        var specialist = Position.Create(TestGuids.CompanyA, "SPC", "Specialist", parentPositionId: manager.Id);
        _db.Positions.AddRange(manager, specialist);
        _db.Grants.Add(Grant.Create(SubjectType.Position, specialist.Id, _perm.Id, SourceType.Position, specialist.Id, Effect.Deny, T0, null, SourcePriority.PositionOverride, scopeUnitId: TestGuids.CompanyA));
        await _db.SaveChangesAsync();

        var asManager = await _evaluator.EvaluateAsync(AccessRequest.ForUser(UserA, "RESOURCE.READ", manager.Id, TestGuids.CompanyA, T0));
        asManager.Allowed.ShouldBeFalse();
        asManager.Reason.ShouldBe(AccessDecisionReasons.NoGrant);
    }

    [Test]
    public async Task Group5_Company_Boundary()
    {
        AuthorizationTestContext.SeedCompany(_db, TestGuids.CompanyB, "B");
        var posA = Position.Create(TestGuids.CompanyA, "A1", "A1");
        var posB = Position.Create(TestGuids.CompanyB, "B1", "B1");
        _db.Positions.AddRange(posA, posB);
        _db.Grants.Add(Grant.Create(SubjectType.Position, posA.Id, _perm.Id, SourceType.Position, posA.Id, Effect.Allow, T0, null, SourcePriority.PositionOverride, scopeUnitId: TestGuids.CompanyA));
        await _db.SaveChangesAsync();

        var d = await _evaluator.EvaluateAsync(AccessRequest.ForUser(UserA, "RESOURCE.READ", posB.Id, TestGuids.CompanyB, T0));
        d.Allowed.ShouldBeFalse();
    }

    [Test]
    public async Task Group6_Individual_Grant_Does_Not_Follow_Position()
    {
        var pos = Position.Create(TestGuids.CompanyA, "P", "P");
        _db.Positions.Add(pos);
        _db.Grants.Add(Grant.CreateForUser(UserA, _perm.Id, SourceType.User, UserA, Effect.Allow, T0, null, SourcePriority.IndividualOverride, scopeUnitId: TestGuids.CompanyA));
        await _db.SaveChangesAsync();

        var other = Guid.Parse("bbbbbbbb-0000-0000-0000-000000000002");
        var d = await _evaluator.EvaluateAsync(AccessRequest.ForUser(other, "RESOURCE.READ", pos.Id, TestGuids.CompanyA, T0));
        d.Allowed.ShouldBeFalse();
    }

    [Test]
    public async Task GetAccessibleScopes_Returns_Grant_Roots()
    {
        _db.Grants.Add(Grant.CreateForUser(UserA, _perm.Id, SourceType.User, UserA, Effect.Allow, T0, null, SourcePriority.IndividualOverride, scopeUnitId: TestGuids.CompanyA));
        await _db.SaveChangesAsync();

        var scopes = await _evaluator.GetAccessibleScopesAsync(UserA, null, "RESOURCE.READ");
        scopes.IsUnrestricted.ShouldBeFalse();
        scopes.ScopeRootUnitIds.ShouldContain(TestGuids.CompanyA);
    }
}
