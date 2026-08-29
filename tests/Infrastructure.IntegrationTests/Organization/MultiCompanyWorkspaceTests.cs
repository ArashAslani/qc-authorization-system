using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using qc_authorization.Application.Authorization.Evaluation;
using qc_authorization.Application.Common.Interfaces;
using qc_authorization.Domain.Authorization;
using qc_authorization.Domain.Authorization.Enums;
using qc_authorization.Domain.Authorization.Evaluation;
using qc_authorization.Domain.Authorization.Services;
using qc_authorization.Domain.Authorization.ValueObjects;
using qc_authorization.Domain.Organization;
using qc_authorization.Infrastructure.Data;
using qc_authorization.Infrastructure.IntegrationTests.TestSupport;
using NUnit.Framework;
using Shouldly;

namespace qc_authorization.Infrastructure.IntegrationTests.Organization;

using qc_authorization.Tests.TestSupport;

[TestFixture]
public class MultiCompanyWorkspaceTests
{
    private ApplicationDbContext _context = null!;
    private AccessEvaluator _evaluator = null!;
    private Permission _perm = null!;
    private static readonly DateTimeOffset T0 = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private Position _posA1 = null!;
    private Position _posA2 = null!;
    private Position _posB1 = null!;
    private Personnel _personnel = null!;
    private Guid _userId = Guid.NewGuid();

    [SetUp]
    public async Task SetUp()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"qc-multi-co-{Guid.NewGuid():N}")
            .Options;
        _context = new ApplicationDbContext(options);
        await _context.Database.EnsureCreatedAsync();

        _posA1 = Position.Create(TestGuids.CompanyA, "TECH_COMMITTEE", "Technical Committee Manager");
        _posA1.Id = TestGuids.PosA1;
        _posA2 = Position.Create(TestGuids.CompanyA, "DEV_HEAD", "Development Unit Head");
        _posA2.Id = TestGuids.PosA2;
        _posB1 = Position.Create(TestGuids.CompanyB, "SALES_MGR", "Sales Manager");
        _posB1.Id = TestGuids.PosB1;

        _context.Positions.AddRange(_posA1, _posA2, _posB1);

        _perm = Permission.Create("CONTROL_PLAN.APPROVE", "CONTROL_PLAN", "APPROVE");
        _context.Permissions.Add(_perm);

        _personnel = Personnel.Create("0012345678", "Ali", "Ahmadi", "PC-001", identityUserId: _userId);
        _context.Personnel.Add(_personnel);

        var assignA1 = PositionAssignment.Create(_personnel.Id, _posA1.Id, T0.AddDays(-30));
        assignA1.Id = TestGuids.Assignment101;
        var assignA2 = PositionAssignment.Create(_personnel.Id, _posA2.Id, T0.AddDays(-30));
        assignA2.Id = TestGuids.Assignment102;
        var assignB1 = PositionAssignment.Create(_personnel.Id, _posB1.Id, T0.AddDays(-30));
        assignB1.Id = TestGuids.Assignment103;
        assignB1.MarkAsPrimary();

        _context.PositionAssignments.AddRange(assignA1, assignA2, assignB1);

        _context.Grants.Add(Grant.Create(
            SubjectType.Position, _posA1.Id, _perm.Id, SourceType.Position, _posA1.Id,
            Effect.Allow, T0.AddDays(-30), null, SourcePriority.RoleOrRoleGroup));
        _context.Grants.Add(Grant.Create(
            SubjectType.Position, _posA2.Id, _perm.Id, SourceType.Position, _posA2.Id,
            Effect.Allow, T0.AddDays(-30), null, SourcePriority.RoleOrRoleGroup));
        _context.Grants.Add(Grant.Create(
            SubjectType.Position, _posB1.Id, _perm.Id, SourceType.Position, _posB1.Id,
            Effect.Allow, T0.AddDays(-30), null, SourcePriority.RoleOrRoleGroup));

        await _context.SaveChangesAsync();

        var hierarchy = new PositionHierarchyService();
        var applicability = new GrantApplicabilityService(hierarchy);
        var engine = new AccessEvaluationEngine();

        _evaluator = new AccessEvaluator(
            new PositionAwareCandidateGrantResolver(
                _context,
                applicability,
                new StaticCurrentUser(_userId, _personnel.Id, activeCompanyId: TestGuids.CompanyA)),
            engine);
    }

    [TearDown]
    public async Task TearDown()
    {
        await _context.Database.EnsureDeletedAsync();
        await _context.DisposeAsync();
    }

    [Test]
    public async Task CompanyA_Context_Unions_Both_Positions_In_Same_Company()
    {
        var context = new Dictionary<string, object> { ["CompanyId"] = TestGuids.CompanyA };
        var decision = await _evaluator.EvaluateAsync(
            AccessRequest.ForUser(_userId, "APPROVE", "CONTROL_PLAN", null, T0, context));

        decision.Effect.ShouldBe(Effect.Allow);
        decision.Trace.ApplicableGrants.Count.ShouldBe(2);
    }

    [Test]
    public async Task CompanyB_Context_Only_Includes_B_Positions()
    {
        var resolver = new PositionAwareCandidateGrantResolver(
            _context,
            new GrantApplicabilityService(new PositionHierarchyService()),
            new StaticCurrentUser(_userId, _personnel.Id, activeCompanyId: TestGuids.CompanyB));

        var evaluator = new AccessEvaluator(resolver, new AccessEvaluationEngine());

        var context = new Dictionary<string, object> { ["CompanyId"] = TestGuids.CompanyB };
        var decision = await evaluator.EvaluateAsync(
            AccessRequest.ForUser(_userId, "APPROVE", "CONTROL_PLAN", null, T0, context));

        decision.Effect.ShouldBe(Effect.Allow);
        decision.Trace.ApplicableGrants.Count.ShouldBe(1);
        decision.Trace.ApplicableGrants[0].SourceId.ShouldBe(_posB1.Id);
    }

    [Test]
    public async Task No_Active_Company_Denies_Position_Grants()
    {
        var resolver = new PositionAwareCandidateGrantResolver(
            _context,
            new GrantApplicabilityService(new PositionHierarchyService()),
            new StaticCurrentUser(_userId, _personnel.Id, activeCompanyId: null));

        var evaluator = new AccessEvaluator(resolver, new AccessEvaluationEngine());

        var decision = await evaluator.EvaluateAsync(
            AccessRequest.ForUser(_userId, "APPROVE", "CONTROL_PLAN", null, T0));

        decision.Effect.ShouldBe(Effect.Deny);
    }
}
