using AccessManagement.Application.Abstractions;
using AccessManagement.Application.Authorization.Evaluation;
using AccessManagement.Application.Organization;
using AccessManagement.Domain.Authorization;
using AccessManagement.Domain.Authorization.Enums;
using AccessManagement.Domain.Authorization.Evaluation;
using AccessManagement.Domain.Authorization.Services;
using AccessManagement.Domain.Authorization.ValueObjects;
using AccessManagement.Domain.Organization;
using AccessManagement.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using Qc.AccessPlugin;
using Qc.AccessPlugin.ControlPlans;
using Shouldly;

namespace Qc.AccessPlugin.Tests;

/// <summary>
/// Transferable QC scenario fixtures. Not production data — copy the pattern
/// into the QC product and fill it with real units and grants.
/// </summary>
[TestFixture]
public class QcAccessScenarioTests
{
    private static readonly DateTimeOffset T0 = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
    private static readonly Guid UserA = Guid.Parse("aaaaaaaa-0000-0000-0000-00000000aa01");

    private ApplicationDbContext _db = null!;
    private IAccessEvaluator _evaluator = null!;
    private ControlPlanApprovalGuard _guard = null!;
    private OrganizationalUnit _holding = null!;
    private OrganizationalUnit _companyA = null!;
    private OrganizationalUnit _companyB = null!;
    private OrganizationalUnit _stationA = null!;
    private Permission _approve = null!;
    private Permission _labRead = null!;

    [SetUp]
    public async Task SetUp()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"qc-plugin-{Guid.NewGuid():N}")
            .EnableServiceProviderCaching(false)
            .Options;

        _db = new ApplicationDbContext(options);
        await _db.Database.EnsureCreatedAsync();

        await new QcAccessSeeder().SeedAsync(_db);

        _holding = OrganizationalUnit.Create(OrganizationalUnitTypes.Holding, "Holding");
        _companyA = OrganizationalUnit.Create(OrganizationalUnitTypes.Company, "Company A", _holding.Id);
        _companyB = OrganizationalUnit.Create(OrganizationalUnitTypes.Company, "Company B", _holding.Id);
        _stationA = OrganizationalUnit.Create(QcPermissions.SuggestedUnitTypes.Workstation, "Station A", _companyA.Id);
        _db.OrganizationalUnits.AddRange(_holding, _companyA, _companyB, _stationA);
        await _db.SaveChangesAsync();

        _approve = _db.Permissions.Single(p => p.Code == QcPermissions.ControlPlanApprove);
        _labRead = _db.Permissions.Single(p => p.Code == QcPermissions.LaboratoryRead);

        var hierarchy = new PositionHierarchyService();
        var resolver = new GrantResolver(_db, new GrantApplicabilityService(hierarchy), new CatalogGrantFilter(_db), new PositionHierarchyQuery(_db, hierarchy));
        _evaluator = new AccessEvaluator(resolver, new ScopeMatcher(new OrganizationalUnitHierarchyService(_db)), new NullDecisionTraceWriter());
        _guard = new ControlPlanApprovalGuard(_evaluator);
    }

    [TearDown]
    public async Task TearDown()
    {
        await _db.Database.EnsureDeletedAsync();
        await _db.DisposeAsync();
    }

    [Test]
    public void Seeder_Writes_Qc_Permissions_And_ModuleScope()
    {
        _db.Permissions.Count(p => p.PluginCode == QcPermissions.PluginCode).ShouldBe(6);
        _db.ModuleScopeConfigs.Single(c => c.ResourceCode == "LABORATORY").MaxScopeUnitType
            .ShouldBe(QcPermissions.SuggestedUnitTypes.Workstation);
        _db.ModuleScopeConfigs.Single(c => c.ResourceCode == "CONTROLPLAN").MaxScopeUnitType
            .ShouldBe(OrganizationalUnitTypes.Company);
    }

    [Test]
    public async Task Holding_Grant_Matches_CompanyA_Resource()
    {
        _db.Grants.Add(Grant.CreateForUser(UserA, _approve.Id, SourceType.User, UserA, Effect.Allow, T0, null, SourcePriority.IndividualOverride, scopeUnitId: _holding.Id));
        await _db.SaveChangesAsync();

        var d = await _evaluator.EvaluateAsync(AccessRequest.ForUser(UserA, QcPermissions.ControlPlanApprove, resourceScopeUnitId: _companyA.Id, when: T0));
        d.Allowed.ShouldBeTrue();
    }

    [Test]
    public async Task CompanyA_Grant_Does_Not_Match_Holding_Resource()
    {
        _db.Grants.Add(Grant.CreateForUser(UserA, _approve.Id, SourceType.User, UserA, Effect.Allow, T0, null, SourcePriority.IndividualOverride, scopeUnitId: _companyA.Id));
        await _db.SaveChangesAsync();

        var d = await _evaluator.EvaluateAsync(AccessRequest.ForUser(UserA, QcPermissions.ControlPlanApprove, resourceScopeUnitId: _holding.Id, when: T0));
        d.Allowed.ShouldBeFalse();
    }

    [Test]
    public async Task CompanyA_Grant_Does_Not_Match_CompanyB_Workstation()
    {
        var stationB = OrganizationalUnit.Create(QcPermissions.SuggestedUnitTypes.Workstation, "Station B", _companyB.Id);
        _db.OrganizationalUnits.Add(stationB);
        _db.Grants.Add(Grant.CreateForUser(UserA, _labRead.Id, SourceType.User, UserA, Effect.Allow, T0, null, SourcePriority.IndividualOverride, scopeUnitId: _companyA.Id));
        await _db.SaveChangesAsync();

        var d = await _evaluator.EvaluateAsync(AccessRequest.ForUser(UserA, QcPermissions.LaboratoryRead, resourceScopeUnitId: stationB.Id, when: T0));
        d.Allowed.ShouldBeFalse();
    }

    [Test]
    public async Task Narrow_Workstation_Grant_Does_Not_Match_Parent_Company()
    {
        _db.Grants.Add(Grant.CreateForUser(UserA, _labRead.Id, SourceType.User, UserA, Effect.Allow, T0, null, SourcePriority.IndividualOverride, scopeUnitId: _stationA.Id));
        await _db.SaveChangesAsync();

        var d = await _evaluator.EvaluateAsync(AccessRequest.ForUser(UserA, QcPermissions.LaboratoryRead, resourceScopeUnitId: _companyA.Id, when: T0));
        d.Allowed.ShouldBeFalse();
    }

    [Test]
    public async Task Guard_Blocks_Draft_Even_When_Engine_Allows()
    {
        _db.Grants.Add(Grant.CreateForUser(UserA, _approve.Id, SourceType.User, UserA, Effect.Allow, T0, null, SourcePriority.IndividualOverride, scopeUnitId: _companyA.Id));
        await _db.SaveChangesAsync();

        var draft = ControlPlan.Create(Guid.NewGuid(), "CP-1", "Draft plan", _companyA.Id);
        await Should.ThrowAsync<InvalidOperationException>(() =>
            _guard.EnsureCanApproveAsync(draft, UserA, null));
    }

    [Test]
    public async Task Guard_Allows_UnderReview_When_Grant_Matches()
    {
        _db.Grants.Add(Grant.CreateForUser(UserA, _approve.Id, SourceType.User, UserA, Effect.Allow, T0, null, SourcePriority.IndividualOverride, scopeUnitId: _companyA.Id));
        await _db.SaveChangesAsync();

        var plan = ControlPlan.Create(Guid.NewGuid(), "CP-2", "Ready", _companyA.Id);
        plan.SubmitForReview();

        await Should.NotThrowAsync(() => _guard.EnsureCanApproveAsync(plan, UserA, null));
    }

    [Test]
    public async Task Subordinate_Position_Allow_Propagates_To_Manager()
    {
        var manager = Position.Create(_companyA.Id, "MGR", "Manager");
        var specialist = Position.Create(_companyA.Id, "SPC", "Specialist", parentPositionId: manager.Id);
        _db.Positions.AddRange(manager, specialist);
        _db.Grants.Add(Grant.Create(SubjectType.Position, specialist.Id, _approve.Id, SourceType.Position, specialist.Id, Effect.Allow, T0, null, SourcePriority.PositionOverride, scopeUnitId: _companyA.Id));
        await _db.SaveChangesAsync();

        var d = await _evaluator.EvaluateAsync(AccessRequest.ForUser(UserA, QcPermissions.ControlPlanApprove, manager.Id, _companyA.Id, T0));
        d.Allowed.ShouldBeTrue();
    }
}
