using Microsoft.EntityFrameworkCore;
using AccessManagement.Application.Abstractions;
using AccessManagement.Application.Authorization.Evaluation;
using AccessManagement.Application.Organization;
using AccessManagement.Application.Session;
using AccessManagement.Domain.Authorization;
using AccessManagement.Domain.Authorization.Enums;
using AccessManagement.Domain.Authorization.Services;
using AccessManagement.Domain.Authorization.ValueObjects;
using AccessManagement.Domain.Organization;
using AccessManagement.Infrastructure.Data;
using NUnit.Framework;
using Shouldly;

namespace AccessManagement.Infrastructure.IntegrationTests.Organization;

using AccessManagement.Tests.TestSupport;

[TestFixture]
public class MultiCompanyWorkspaceTests
{
    private ApplicationDbContext _context = null!;
    private CompanyWorkspaceService _workspace = null!;
    private static readonly DateTimeOffset T0 = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
    private Guid _userId = Guid.NewGuid();

    [SetUp]
    public async Task SetUp()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"qc-multi-co-{Guid.NewGuid():N}")
            .Options;
        _context = new ApplicationDbContext(options);
        await _context.Database.EnsureCreatedAsync();

        var companyA = OrganizationalUnit.Create(OrganizationalUnitTypes.Company, "A");
        companyA.Id = TestGuids.CompanyA;
        var companyB = OrganizationalUnit.Create(OrganizationalUnitTypes.Company, "B");
        companyB.Id = TestGuids.CompanyB;
        _context.OrganizationalUnits.AddRange(companyA, companyB);

        var posA1 = Position.Create(TestGuids.CompanyA, "TECH_COMMITTEE", "Technical Committee Manager");
        posA1.Id = TestGuids.PosA1;
        var posB1 = Position.Create(TestGuids.CompanyB, "SALES_MGR", "Sales Manager");
        posB1.Id = TestGuids.PosB1;
        _context.Positions.AddRange(posA1, posB1);

        var perm = Permission.Create("CONTROL_PLAN.APPROVE", "CONTROL_PLAN", "APPROVE");
        _context.Permissions.Add(perm);

        var personnel = Personnel.Create("0012345678", "Ali", "Ahmadi", "PC-001", identityUserId: _userId);
        _context.Personnel.Add(personnel);
        _context.PositionAssignments.Add(PositionAssignment.Create(personnel.Id, posA1.Id, T0.AddDays(-30)));
        _context.PositionAssignments.Add(PositionAssignment.Create(personnel.Id, posB1.Id, T0.AddDays(-30)));

        _context.Grants.Add(Grant.Create(
            SubjectType.Position, posA1.Id, perm.Id, SourceType.Position, posA1.Id,
            Effect.Allow, T0.AddDays(-30), null, SourcePriority.RoleOrRoleGroup, scopeUnitId: TestGuids.CompanyA));
        _context.Grants.Add(Grant.Create(
            SubjectType.Position, posB1.Id, perm.Id, SourceType.Position, posB1.Id,
            Effect.Allow, T0.AddDays(-30), null, SourcePriority.RoleOrRoleGroup, scopeUnitId: TestGuids.CompanyB));
        await _context.SaveChangesAsync();

        var hierarchy = new PositionHierarchyService();
        var resolver = new GrantResolver(_context, new GrantApplicabilityService(hierarchy), new CatalogGrantFilter(_context));
        var evaluator = new AccessEvaluator(resolver, new ScopeMatcher(new OrganizationalUnitHierarchyService(_context)), new NullDecisionTraceWriter());
        _workspace = new CompanyWorkspaceService(evaluator, _context);
    }

    [TearDown]
    public async Task TearDown()
    {
        await _context.Database.EnsureDeletedAsync();
        await _context.DisposeAsync();
    }

    [Test]
    public async Task Group7_Union_Within_Company()
    {
        (await _workspace.HasPermissionAsync(_userId, TestGuids.CompanyA, "CONTROL_PLAN.APPROVE", TestGuids.CompanyA))
            .ShouldBeTrue();
    }

    [Test]
    public async Task Group7_Switch_Company_Isolates_Access()
    {
        (await _workspace.HasPermissionAsync(_userId, TestGuids.CompanyA, "CONTROL_PLAN.APPROVE", TestGuids.CompanyB))
            .ShouldBeFalse();
        (await _workspace.HasPermissionAsync(_userId, TestGuids.CompanyB, "CONTROL_PLAN.APPROVE", TestGuids.CompanyB))
            .ShouldBeTrue();
    }
}
