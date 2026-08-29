using qc_authorization.Application.Authorization.Evaluation;
using qc_authorization.Application.UnitTests.TestSupport;
using qc_authorization.Domain.Authorization;
using qc_authorization.Domain.Authorization.Enums;
using qc_authorization.Domain.Authorization.Evaluation;
using qc_authorization.Domain.Authorization.ValueObjects;
using qc_authorization.Domain.Organization;
using qc_authorization.Infrastructure.Data;
using qc_authorization.Tests.TestSupport;
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
        (_context, _evaluator) = AuthorizationTestContext.Create();
        await _context.Database.EnsureCreatedAsync();

        _perm = Permission.Create("PERSONNEL.READ", "Personnel", "Read");
        _context.Permissions.Add(_perm);

        var position = Position.Create(TestGuids.CompanyA, "TEST-POS", "Test Pos");
        position.Id = TestGuids.Position200;
        _context.Positions.Add(position);
        await _context.SaveChangesAsync();
    }

    [TearDown]
    public async Task TearDown()
    {
        await _context.Database.EnsureDeletedAsync();
        await _context.DisposeAsync();
    }

    [Test]
    public async Task Role_Allow_Returns_Allow()
    {
        _context.Grants.Add(NewGrant(SubjectType.Role, TestGuids.Subject50, Effect.Allow, SourceType.Role, TestGuids.Subject50, SourcePriority.RoleOrRoleGroup));
        await _context.SaveChangesAsync();

        var d = await Evaluate(new AccessRequest(SubjectType.Role, TestGuids.Subject50, null, "Read", "Personnel", null, T0));
        d.Effect.ShouldBe(Effect.Allow);
    }

    [Test]
    public async Task Role_Deny_Returns_Deny()
    {
        _context.Grants.Add(NewGrant(SubjectType.Role, TestGuids.Subject50, Effect.Deny, SourceType.Role, TestGuids.Subject50, SourcePriority.RoleOrRoleGroup));
        await _context.SaveChangesAsync();

        var d = await Evaluate(new AccessRequest(SubjectType.Role, TestGuids.Subject50, null, "Read", "Personnel", null, T0));
        d.Effect.ShouldBe(Effect.Deny);
    }

    [Test]
    public async Task Position_Allow_Returns_Allow()
    {
        _context.Grants.Add(NewGrant(SubjectType.Position, TestGuids.Position200, Effect.Allow, SourceType.Position, TestGuids.Position200, SourcePriority.PositionOverride));
        await _context.SaveChangesAsync();

        var d = await Evaluate(new AccessRequest(SubjectType.Position, TestGuids.Position200, null, "Read", "Personnel", null, T0));
        d.Effect.ShouldBe(Effect.Allow);
    }

    [Test]
    public async Task Position_Deny_Returns_Deny()
    {
        _context.Grants.Add(NewGrant(SubjectType.Position, TestGuids.Position200, Effect.Deny, SourceType.Position, TestGuids.Position200, SourcePriority.PositionOverride));
        await _context.SaveChangesAsync();

        var d = await Evaluate(new AccessRequest(SubjectType.Position, TestGuids.Position200, null, "Read", "Personnel", null, T0));
        d.Effect.ShouldBe(Effect.Deny);
    }

    [Test]
    public async Task User_Direct_Allow_Returns_Allow()
    {
        _context.Grants.Add(NewUserGrant(TestUsers.UserA, Effect.Allow, SourcePriority.IndividualOverride));
        await _context.SaveChangesAsync();

        var d = await Evaluate(AccessRequest.ForUser(TestUsers.UserA, "Read", "Personnel", null, T0));
        d.Effect.ShouldBe(Effect.Allow);
    }

    [Test]
    public async Task User_Direct_Deny_Returns_Deny()
    {
        _context.Grants.Add(NewUserGrant(TestUsers.UserA, Effect.Deny, SourcePriority.IndividualOverride));
        await _context.SaveChangesAsync();

        var d = await Evaluate(AccessRequest.ForUser(TestUsers.UserA, "Read", "Personnel", null, T0));
        d.Effect.ShouldBe(Effect.Deny);
    }

    [Test]
    public async Task Expired_Grant_Denies()
    {
        _context.Grants.Add(NewGrant(SubjectType.Role, TestGuids.Subject50, Effect.Allow, SourceType.Role, TestGuids.Subject50, SourcePriority.RoleOrRoleGroup,
            validFrom: T0.AddDays(-10), validTo: T0.AddDays(-1)));
        await _context.SaveChangesAsync();

        var d = await Evaluate(new AccessRequest(SubjectType.Role, TestGuids.Subject50, null, "Read", "Personnel", null, T0));
        d.Effect.ShouldBe(Effect.Deny);
        d.Reason.ShouldBe(DecisionReason.Expired);
    }

    [Test]
    public async Task Valid_Grant_Allows()
    {
        _context.Grants.Add(NewGrant(SubjectType.Role, TestGuids.Subject50, Effect.Allow, SourceType.Role, TestGuids.Subject50, SourcePriority.RoleOrRoleGroup,
            validFrom: T0.AddDays(-1), validTo: T0.AddDays(7)));
        await _context.SaveChangesAsync();

        var d = await Evaluate(new AccessRequest(SubjectType.Role, TestGuids.Subject50, null, "Read", "Personnel", null, T0));
        d.Effect.ShouldBe(Effect.Allow);
    }

    [Test]
    public async Task In_Scope_Allows()
    {
        _context.Grants.Add(NewGrant(SubjectType.Role, TestGuids.Subject50, Effect.Allow, SourceType.Role, TestGuids.Subject50, SourcePriority.RoleOrRoleGroup,
            scopeKind: ScopeKind.Company, scopeIdentifier: "C-1"));
        await _context.SaveChangesAsync();

        var d = await Evaluate(new AccessRequest(SubjectType.Role, TestGuids.Subject50, null, "Read", "Personnel", "C-1", T0));
        d.Effect.ShouldBe(Effect.Allow);
    }

    [Test]
    public async Task Out_Of_Scope_Denies()
    {
        _context.Grants.Add(NewGrant(SubjectType.Role, TestGuids.Subject50, Effect.Allow, SourceType.Role, TestGuids.Subject50, SourcePriority.RoleOrRoleGroup,
            scopeKind: ScopeKind.Company, scopeIdentifier: "C-1"));
        await _context.SaveChangesAsync();

        var d = await Evaluate(new AccessRequest(SubjectType.Role, TestGuids.Subject50, null, "Read", "Personnel", "C-2", T0));
        d.Effect.ShouldBe(Effect.Deny);
        d.Reason.ShouldBe(DecisionReason.OutOfScope);
    }

    [Test]
    public async Task Multiple_Grants_AreDeterministic()
    {
        _context.Grants.AddRange(
            NewGrant(SubjectType.Role, TestGuids.Subject50, Effect.Allow, SourceType.Role, TestGuids.Subject50, SourcePriority.RoleOrRoleGroup),
            NewGrant(SubjectType.Role, TestGuids.Subject50, Effect.Deny, SourceType.Role, TestGuids.Subject50, SourcePriority.PositionOverride));
        await _context.SaveChangesAsync();

        var d1 = await Evaluate(new AccessRequest(SubjectType.Role, TestGuids.Subject50, null, "Read", "Personnel", null, T0));
        var d2 = await Evaluate(new AccessRequest(SubjectType.Role, TestGuids.Subject50, null, "Read", "Personnel", null, T0));
        d1.Effect.ShouldBe(d2.Effect);
    }

    [Test]
    public async Task Higher_Priority_Wins()
    {
        _context.Grants.AddRange(
            NewGrant(SubjectType.Role, TestGuids.Subject50, Effect.Allow, SourceType.Role, TestGuids.Subject50, priority: 10),
            NewGrant(SubjectType.Role, TestGuids.Subject50, Effect.Deny, SourceType.Role, TestGuids.Subject50, priority: 100));
        await _context.SaveChangesAsync();

        var d = await Evaluate(new AccessRequest(SubjectType.Role, TestGuids.Subject50, null, "Read", "Personnel", null, T0));
        d.Effect.ShouldBe(Effect.Deny);
    }

    [Test]
    public async Task Tie_Of_Priority_Resolved_By_Deny_Over_Allow()
    {
        _context.Grants.AddRange(
            NewGrant(SubjectType.Role, TestGuids.Subject50, Effect.Allow, SourceType.Role, TestGuids.Subject50, priority: 50),
            NewGrant(SubjectType.Role, TestGuids.Subject50, Effect.Deny, SourceType.Role, TestGuids.Subject50, priority: 50));
        await _context.SaveChangesAsync();

        var d = await Evaluate(new AccessRequest(SubjectType.Role, TestGuids.Subject50, null, "Read", "Personnel", null, T0));
        d.Effect.ShouldBe(Effect.Deny);
    }

    [Test]
    public async Task Decision_Trace_Contains_AllRequiredFields()
    {
        _context.Grants.AddRange(
            NewGrant(SubjectType.Role, TestGuids.Subject50, Effect.Allow, SourceType.Role, TestGuids.Subject50, SourcePriority.RoleOrRoleGroup),
            NewGrant(SubjectType.Role, TestGuids.Subject50, Effect.Deny, SourceType.Role, TestGuids.Subject50, SourcePriority.PositionOverride));
        await _context.SaveChangesAsync();

        var d = await Evaluate(new AccessRequest(SubjectType.Role, TestGuids.Subject50, null, "Read", "Personnel", "r-1", T0));

        d.Trace.TraceId.ShouldNotBeNullOrEmpty();
        d.Trace.Subject.ShouldBe(SubjectType.Role);
        d.Trace.SubjectId.ShouldBe(TestGuids.Subject50);
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
        var d = await Evaluate(new AccessRequest(SubjectType.Role, TestGuids.Subject50, null, "Read", "Personnel", null, T0));
        d.Effect.ShouldBe(Effect.Deny);
        d.Reason.ShouldBe(DecisionReason.NoCandidateGrants);
        d.Trace.CandidateGrants.Count.ShouldBe(0);
        d.Trace.ApplicableGrants.Count.ShouldBe(0);
    }

    private Task<AccessDecision> Evaluate(AccessRequest r) => _evaluator.EvaluateAsync(r);

    private Grant NewUserGrant(Guid userId, Effect effect, int priority) =>
        Grant.CreateForUser(
            userId,
            _perm.Id,
            SourceType.User,
            Guid.Empty,
            effect,
            T0.AddDays(-1),
            null,
            priority);

    private Grant NewGrant(
        SubjectType subjectType,
        Guid subjectId,
        Effect effect,
        SourceType sourceType,
        Guid sourceId,
        int priority,
        ScopeKind scopeKind = ScopeKind.Unbounded,
        string? scopeIdentifier = null,
        DateTimeOffset? validFrom = null,
        DateTimeOffset? validTo = null) =>
        Grant.Create(
            subjectType,
            subjectId,
            _perm.Id,
            sourceType,
            sourceId,
            effect,
            validFrom ?? T0.AddDays(-1),
            validTo,
            priority,
            scopeKind: scopeKind,
            scopeIdentifier: scopeIdentifier);
}
