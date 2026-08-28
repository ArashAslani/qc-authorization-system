using Microsoft.EntityFrameworkCore;
using qc_authorization.Application.Authorization.Evaluation;
using qc_authorization.Application.Common.Interfaces;
using qc_authorization.Domain.Authorization;
using qc_authorization.Domain.Authorization.Enums;
using qc_authorization.Domain.Authorization.ValueObjects;
using qc_authorization.Domain.Organization;
using qc_authorization.Infrastructure.Data;
using NUnit.Framework;
using Shouldly;

namespace qc_authorization.Application.UnitTests.Authorization;

[TestFixture]
public class AccessEvaluatorTests
{
    private ApplicationDbContext _context = null!;
    private AccessEvaluator _evaluator = null!;
    private Permission _perm = null!;
    private static readonly DateTimeOffset T0 = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    [SetUp]
    public async Task SetUp()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"qc-eval-{Guid.NewGuid()}")
            .Options;
        _context = new ApplicationDbContext(options);
        await _context.Database.EnsureCreatedAsync();

        _perm = new Permission
        {
            Code = "PERSONNEL.READ",
            Resource = "Personnel",
            Action = "Read",
        };
        _context.Permissions.Add(_perm);

        // Seed a Position so position-typed tests have something to resolve.
        _context.Positions.Add(new Position { Id = 200, Code = "TEST-POS", Name = "Test Pos" });
        await _context.SaveChangesAsync();

        _evaluator = new AccessEvaluator(new PositionAwareCandidateGrantResolver(_context, new PositionHierarchyService()));
    }

    [TearDown]
    public async Task TearDown()
    {
        await _context.Database.EnsureDeletedAsync();
        await _context.DisposeAsync();
    }

    // --- role allow / deny ---
    [Test]
    public async Task Role_Allow_Returns_Allow()
    {
        _context.Grants.Add(NewGrant(SubjectType.Role, 50, Effect.Allow, SourceType.Role, 50, SourcePriority.RoleOrRoleGroup));
        await _context.SaveChangesAsync();

        var d = await Evaluate(new AccessRequest(SubjectType.Role, 50, "Read", "Personnel", null, T0));
        d.Effect.ShouldBe(Effect.Allow);
    }

    [Test]
    public async Task Role_Deny_Returns_Deny()
    {
        _context.Grants.Add(NewGrant(SubjectType.Role, 50, Effect.Deny, SourceType.Role, 50, SourcePriority.RoleOrRoleGroup));
        await _context.SaveChangesAsync();

        var d = await Evaluate(new AccessRequest(SubjectType.Role, 50, "Read", "Personnel", null, T0));
        d.Effect.ShouldBe(Effect.Deny);
    }

    // --- position allow / deny ---
    [Test]
    public async Task Position_Allow_Returns_Allow()
    {
        _context.Grants.Add(NewGrant(SubjectType.Position, 200, Effect.Allow, SourceType.Position, 200, SourcePriority.PositionOverride));
        await _context.SaveChangesAsync();

        var d = await Evaluate(new AccessRequest(SubjectType.Position, 200, "Read", "Personnel", null, T0));
        d.Effect.ShouldBe(Effect.Allow);
    }

    [Test]
    public async Task Position_Deny_Returns_Deny()
    {
        _context.Grants.Add(NewGrant(SubjectType.Position, 200, Effect.Deny, SourceType.Position, 200, SourcePriority.PositionOverride));
        await _context.SaveChangesAsync();

        var d = await Evaluate(new AccessRequest(SubjectType.Position, 200, "Read", "Personnel", null, T0));
        d.Effect.ShouldBe(Effect.Deny);
    }

    // --- user direct allow / deny ---
    [Test]
    public async Task User_Direct_Allow_Returns_Allow()
    {
        _context.Grants.Add(NewGrant(SubjectType.User, 80, Effect.Allow, SourceType.User, 80, SourcePriority.IndividualOverride));
        await _context.SaveChangesAsync();

        var d = await Evaluate(new AccessRequest(SubjectType.User, 80, "Read", "Personnel", null, T0));
        d.Effect.ShouldBe(Effect.Allow);
    }

    [Test]
    public async Task User_Direct_Deny_Returns_Deny()
    {
        _context.Grants.Add(NewGrant(SubjectType.User, 80, Effect.Deny, SourceType.User, 80, SourcePriority.IndividualOverride));
        await _context.SaveChangesAsync();

        var d = await Evaluate(new AccessRequest(SubjectType.User, 80, "Read", "Personnel", null, T0));
        d.Effect.ShouldBe(Effect.Deny);
    }

    // --- expired / valid ---
    [Test]
    public async Task Expired_Grant_Denies()
    {
        _context.Grants.Add(NewGrant(SubjectType.Role, 50, Effect.Allow, SourceType.Role, 50, SourcePriority.RoleOrRoleGroup,
            validFrom: T0.AddDays(-10), validTo: T0.AddDays(-1)));
        await _context.SaveChangesAsync();

        var d = await Evaluate(new AccessRequest(SubjectType.Role, 50, "Read", "Personnel", null, T0));
        d.Effect.ShouldBe(Effect.Deny);
        d.Reason.ShouldBe(DecisionReason.Expired);
    }

    [Test]
    public async Task Valid_Grant_Allows()
    {
        _context.Grants.Add(NewGrant(SubjectType.Role, 50, Effect.Allow, SourceType.Role, 50, SourcePriority.RoleOrRoleGroup,
            validFrom: T0.AddDays(-1), validTo: T0.AddDays(7)));
        await _context.SaveChangesAsync();

        var d = await Evaluate(new AccessRequest(SubjectType.Role, 50, "Read", "Personnel", null, T0));
        d.Effect.ShouldBe(Effect.Allow);
    }

    // --- in-scope / out-of-scope ---
    [Test]
    public async Task In_Scope_Allows()
    {
        _context.Grants.Add(NewGrant(SubjectType.Role, 50, Effect.Allow, SourceType.Role, 50, SourcePriority.RoleOrRoleGroup,
            scopeKind: ScopeKind.Company, scopeIdentifier: "C-1"));
        await _context.SaveChangesAsync();

        var d = await Evaluate(new AccessRequest(SubjectType.Role, 50, "Read", "Personnel", "C-1", T0));
        d.Effect.ShouldBe(Effect.Allow);
    }

    [Test]
    public async Task Out_Of_Scope_Denies()
    {
        _context.Grants.Add(NewGrant(SubjectType.Role, 50, Effect.Allow, SourceType.Role, 50, SourcePriority.RoleOrRoleGroup,
            scopeKind: ScopeKind.Company, scopeIdentifier: "C-1"));
        await _context.SaveChangesAsync();

        var d = await Evaluate(new AccessRequest(SubjectType.Role, 50, "Read", "Personnel", "C-2", T0));
        d.Effect.ShouldBe(Effect.Deny);
        d.Reason.ShouldBe(DecisionReason.OutOfScope);
    }

    // --- multiple grants ---
    [Test]
    public async Task Multiple_Grants_AreDeterministic()
    {
        _context.Grants.AddRange(
            NewGrant(SubjectType.Role, 50, Effect.Allow, SourceType.Role, 50, SourcePriority.RoleOrRoleGroup),
            NewGrant(SubjectType.Role, 50, Effect.Deny, SourceType.Role, 50, SourcePriority.PositionOverride));
        await _context.SaveChangesAsync();

        var d1 = await Evaluate(new AccessRequest(SubjectType.Role, 50, "Read", "Personnel", null, T0));
        var d2 = await Evaluate(new AccessRequest(SubjectType.Role, 50, "Read", "Personnel", null, T0));
        d1.Effect.ShouldBe(d2.Effect);
    }

    // --- priority ---
    [Test]
    public async Task Higher_Priority_Wins()
    {
        _context.Grants.AddRange(
            NewGrant(SubjectType.Role, 50, Effect.Allow, SourceType.Role, 50, priority: 10),
            NewGrant(SubjectType.Role, 50, Effect.Deny, SourceType.Role, 50, priority: 100));
        await _context.SaveChangesAsync();

        var d = await Evaluate(new AccessRequest(SubjectType.Role, 50, "Read", "Personnel", null, T0));
        d.Effect.ShouldBe(Effect.Deny);
    }

    [Test]
    public async Task Tie_Of_Priority_Resolved_By_Deny_Over_Allow()
    {
        _context.Grants.AddRange(
            NewGrant(SubjectType.Role, 50, Effect.Allow, SourceType.Role, 50, priority: 50),
            NewGrant(SubjectType.Role, 50, Effect.Deny, SourceType.Role, 50, priority: 50));
        await _context.SaveChangesAsync();

        var d = await Evaluate(new AccessRequest(SubjectType.Role, 50, "Read", "Personnel", null, T0));
        d.Effect.ShouldBe(Effect.Deny);
    }

    // --- decision trace ---
    [Test]
    public async Task Decision_Trace_Contains_AllRequiredFields()
    {
        _context.Grants.AddRange(
            NewGrant(SubjectType.Role, 50, Effect.Allow, SourceType.Role, 50, SourcePriority.RoleOrRoleGroup),
            NewGrant(SubjectType.Role, 50, Effect.Deny, SourceType.Role, 50, SourcePriority.PositionOverride));
        await _context.SaveChangesAsync();

        var d = await Evaluate(new AccessRequest(SubjectType.Role, 50, "Read", "Personnel", "r-1", T0));

        d.Trace.TraceId.ShouldNotBeNullOrEmpty();
        d.Trace.Subject.ShouldBe(SubjectType.Role);
        d.Trace.SubjectId.ShouldBe(50);
        d.Trace.RequestedPermission.ShouldBe("Personnel.Read");
        d.Trace.Resource.ShouldBe("Personnel");
        d.Trace.ResourceId.ShouldBe("r-1");
        d.Trace.CandidateGrants.Count.ShouldBe(2);
        d.Trace.ApplicableGrants.Count.ShouldBe(2);
        d.Trace.ConflictResolution.Count.ShouldBe(2);
        d.Trace.ConflictResolution.Single(c => c.Won).Effect.ShouldBe(Effect.Deny);
        d.Trace.FinalDecision.ShouldBe(Effect.Deny);
        d.Trace.Reason.ShouldNotBeNullOrEmpty();
    }

    [Test]
    public async Task No_Candidate_Grants_Denies_And_Traces_EmptyCandidates()
    {
        var d = await Evaluate(new AccessRequest(SubjectType.Role, 50, "Read", "Personnel", null, T0));
        d.Effect.ShouldBe(Effect.Deny);
        d.Reason.ShouldBe(DecisionReason.NoCandidateGrants);
        d.Trace.CandidateGrants.Count.ShouldBe(0);
        d.Trace.ApplicableGrants.Count.ShouldBe(0);
    }

    private Task<AccessDecision> Evaluate(AccessRequest r) => _evaluator.EvaluateAsync(r);

    private Grant NewGrant(
        SubjectType subjectType,
        int subjectId,
        Effect effect,
        SourceType sourceType,
        int sourceId,
        int priority,
        ScopeKind scopeKind = ScopeKind.Unbounded,
        string? scopeIdentifier = null,
        DateTimeOffset? validFrom = null,
        DateTimeOffset? validTo = null)
    {
        return new Grant
        {
            SubjectType = subjectType,
            SubjectId = subjectId,
            PermissionId = _perm.Id,
            Effect = effect,
            SourceType = sourceType,
            SourceId = sourceId,
            Priority = priority,
            ScopeKind = scopeKind,
            ScopeIdentifier = scopeIdentifier,
            ValidFrom = validFrom ?? T0.AddDays(-1),
            ValidTo = validTo,
        };
    }
}
