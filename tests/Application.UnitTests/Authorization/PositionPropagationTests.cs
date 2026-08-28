using Microsoft.EntityFrameworkCore;
using qc_authorization.Application.Authorization.Evaluation;
using qc_authorization.Application.UnitTests.TestSupport;
using qc_authorization.Domain.Authorization;
using qc_authorization.Domain.Authorization.Enums;
using qc_authorization.Domain.Authorization.Evaluation;
using qc_authorization.Domain.Authorization.ValueObjects;
using qc_authorization.Domain.Organization;
using qc_authorization.Infrastructure.Data;
using NUnit.Framework;
using Shouldly;

namespace qc_authorization.Application.UnitTests.Authorization;

[TestFixture]
[NonParallelizable]
public class PositionPropagationTests
{
    private ApplicationDbContext _context = null!;
    private AccessEvaluator _evaluator = null!;
    private Permission _perm = null!;
    private static readonly DateTimeOffset T0 = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private Position _a = null!;
    private Position _b = null!;
    private Position _c = null!;
    private Position _d = null!;

    [SetUp]
    public async Task SetUp()
    {
        (_context, _evaluator) = AuthorizationTestContext.Create();
        await _context.Database.EnsureCreatedAsync();

        _a = PositionWithId(1, "A", "A");
        _b = PositionWithId(2, "B", "B", 1);
        _c = PositionWithId(3, "C", "C", 2);
        _d = PositionWithId(4, "D", "D", 1);
        _context.Positions.AddRange(_a, _b, _c, _d);

        _perm = Permission.Create("PERSONNEL.READ", "Personnel", "Read");
        _context.Permissions.Add(_perm);
        await _context.SaveChangesAsync();
    }

    [TearDown]
    public async Task TearDown()
    {
        await _context.Database.EnsureDeletedAsync();
        await _context.DisposeAsync();
    }

    [Test]
    public async Task Position_Allow_On_C_Affects_User_In_C_B_And_A()
    {
        _context.Grants.Add(NewGrant(SubjectType.Position, _c.Id, Effect.Allow, SourceType.Position, _c.Id, SourcePriority.RoleOrRoleGroup));
        await _context.SaveChangesAsync();

        AssignUser(80, _c.Id);
        AssignUser(81, _b.Id);
        AssignUser(82, _a.Id);
        AssignUser(83, _d.Id);
        await _context.SaveChangesAsync();

        (await EvaluateForUser(80)).Effect.ShouldBe(Effect.Allow, "user in C");
        (await EvaluateForUser(81)).Effect.ShouldBe(Effect.Allow, "user in B (C's ancestor)");
        (await EvaluateForUser(82)).Effect.ShouldBe(Effect.Allow, "user in A (C's ancestor)");
        (await EvaluateForUser(83)).Effect.ShouldBe(Effect.Deny, "user in D (sibling, not in propagation path)");
    }

    [Test]
    public async Task Position_Allow_On_C_Does_Not_Propagate_To_Sibling_D()
    {
        _context.Grants.Add(NewGrant(SubjectType.Position, _c.Id, Effect.Allow, SourceType.Position, _c.Id, SourcePriority.RoleOrRoleGroup));
        AssignUser(83, _d.Id);
        await _context.SaveChangesAsync();

        (await EvaluateForUser(83)).Effect.ShouldBe(Effect.Deny);
    }

    [Test]
    public async Task Position_Deny_On_B_Affects_User_In_B_C_But_Not_A()
    {
        _context.Grants.Add(NewGrant(SubjectType.Position, _b.Id, Effect.Deny, SourceType.Position, _b.Id, SourcePriority.RoleOrRoleGroup));
        AssignUser(80, _b.Id);
        AssignUser(81, _c.Id);
        AssignUser(82, _a.Id);
        await _context.SaveChangesAsync();

        (await EvaluateForUser(80)).Effect.ShouldBe(Effect.Deny, "user in B");
        (await EvaluateForUser(81)).Effect.ShouldBe(Effect.Deny, "user in C (B's descendant)");
        (await EvaluateForUser(82)).Effect.ShouldBe(Effect.Deny, "user in A has no applicable grant when deny is only on descendant B");
    }

    [Test]
    public async Task Position_Deny_On_B_Does_Not_Propagate_To_Ancestor_A()
    {
        _context.Grants.Add(NewGrant(SubjectType.User, 82, Effect.Allow, SourceType.User, 82, SourcePriority.IndividualOverride));
        _context.Grants.Add(NewGrant(SubjectType.Position, _b.Id, Effect.Deny, SourceType.Position, _b.Id, SourcePriority.RoleOrRoleGroup));
        AssignUser(82, _a.Id);
        await _context.SaveChangesAsync();

        (await EvaluateForUser(82)).Effect.ShouldBe(Effect.Allow, "ancestor must not receive propagated deny from B");
    }

    [Test]
    public async Task Re_Parenting_C_From_B_To_D_Changes_Propagation()
    {
        _context.Grants.Add(NewGrant(SubjectType.Position, _c.Id, Effect.Allow, SourceType.Position, _c.Id, SourcePriority.RoleOrRoleGroup));
        AssignUser(81, _b.Id);
        AssignUser(83, _d.Id);
        await _context.SaveChangesAsync();

        (await EvaluateForUser(81)).Effect.ShouldBe(Effect.Allow, "user in B (C's ancestor)");

        var trackedC = await _context.Positions.SingleAsync(p => p.Id == _c.Id);
        var trackedD = await _context.Positions.SingleAsync(p => p.Id == _d.Id);
        var all = await _context.Positions.AsNoTracking().ToListAsync();
        trackedC.Reparent(trackedD, all, new PositionHierarchyService());
        await _context.SaveChangesAsync();

        (await EvaluateForUser(81)).Effect.ShouldBe(Effect.Deny, "C no longer under B; Allow no longer propagates to user in B");
        (await EvaluateForUser(83)).Effect.ShouldBe(Effect.Allow, "Allow now propagates to user in D (C's new ancestor)");
    }

    [Test]
    public async Task Individual_Allow_Does_Not_Propagate_To_Ancestor_Or_Descendant()
    {
        _context.Grants.Add(NewGrant(SubjectType.User, 80, Effect.Allow, SourceType.User, 80, SourcePriority.IndividualOverride));
        _context.Grants.Add(NewGrant(SubjectType.Position, _c.Id, Effect.Deny, SourceType.Position, _c.Id, SourcePriority.RoleOrRoleGroup));
        await _context.SaveChangesAsync();

        (await EvaluateForUser(80)).Effect.ShouldBe(Effect.Allow);
    }

    [Test]
    public async Task User_Changing_Position_Does_Not_Move_Individual_Grant()
    {
        _context.Grants.Add(NewGrant(SubjectType.User, 80, Effect.Allow, SourceType.User, 80, SourcePriority.IndividualOverride));
        _context.Grants.Add(NewGrant(SubjectType.Position, _c.Id, Effect.Deny, SourceType.Position, _c.Id, SourcePriority.RoleOrRoleGroup));
        _context.PositionAssignments.Add(PositionAssignment.Create(
            EnsurePersonnel(80).Id, _c.Id, T0.AddDays(-30)));
        var personnel80 = EnsurePersonnel(80);
        await _context.SaveChangesAsync();

        (await EvaluateForUser(80)).Effect.ShouldBe(Effect.Allow);

        var assignment = await _context.PositionAssignments.SingleAsync(a => a.PersonnelId == personnel80.Id);
        _context.PositionAssignments.Remove(assignment);
        _context.PositionAssignments.Add(PositionAssignment.Create(
            EnsurePersonnel(80).Id, _d.Id, T0));
        await _context.SaveChangesAsync();

        (await EvaluateForUser(80)).Effect.ShouldBe(Effect.Allow,
            "individual grant must remain isolated from position propagation even after the user moves");
    }

    [Test]
    public async Task Individual_Deny_Does_Not_Propagate_To_Ancestor_Position()
    {
        _context.Grants.Add(NewGrant(SubjectType.User, 80, Effect.Deny, SourceType.User, 80, SourcePriority.IndividualOverride));
        _context.Grants.Add(NewGrant(SubjectType.Position, _c.Id, Effect.Allow, SourceType.Position, _c.Id, SourcePriority.RoleOrRoleGroup));
        _context.PositionAssignments.Add(PositionAssignment.Create(
            EnsurePersonnel(80).Id, _c.Id, T0.AddDays(-30)));
        await _context.SaveChangesAsync();

        (await EvaluateForUser(80)).Effect.ShouldBe(Effect.Deny);

        _context.Grants.Add(NewGrant(SubjectType.User, 81, Effect.Allow, SourceType.User, 81, SourcePriority.IndividualOverride));
        _context.PositionAssignments.Add(PositionAssignment.Create(
            EnsurePersonnel(81).Id, _b.Id, T0.AddDays(-30)));
        await _context.SaveChangesAsync();

        (await EvaluateForUser(81)).Effect.ShouldBe(Effect.Allow,
            "another user in B must not be affected by user 80's individual deny");
    }

    private Task<AccessDecision> EvaluateForUser(int userId) =>
        _evaluator.EvaluateAsync(new AccessRequest(SubjectType.User, userId, "Read", "Personnel", null, T0));

    private void AssignUser(int systemUserId, int positionId)
    {
        var personnel = EnsurePersonnel(systemUserId);
        _context.PositionAssignments.Add(PositionAssignment.Create(
            personnel.Id, positionId, T0.AddDays(-30)));
    }

    private Personnel EnsurePersonnel(int systemUserId)
    {
        var existing = _context.Personnel.Local.FirstOrDefault(p => p.SystemUserId == systemUserId)
            ?? _context.Personnel.FirstOrDefault(p => p.SystemUserId == systemUserId);
        if (existing is not null)
        {
            return existing;
        }

        var personnel = Personnel.Create(
            $"NID-{systemUserId}",
            $"User{systemUserId}",
            "Test",
            $"PC-{systemUserId}",
            systemUserId: systemUserId);
        _context.Personnel.Add(personnel);
        return personnel;
    }

    private static Position PositionWithId(int id, string code, string title, int? parentId = null, int companyId = 1)
    {
        var position = Position.Create(companyId, code, title, parentPositionId: parentId);
        position.Id = id;
        return position;
    }

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
