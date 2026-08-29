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

using qc_authorization.Tests.TestSupport;

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

        _a = PositionWithId(Guid.Parse("01000001-0000-0000-0000-000000000001"), "A", "A");
        _b = PositionWithId(Guid.Parse("01000002-0000-0000-0000-000000000002"), "B", "B", _a.Id);
        _c = PositionWithId(Guid.Parse("01000003-0000-0000-0000-000000000003"), "C", "C", _b.Id);
        _d = PositionWithId(Guid.Parse("01000004-0000-0000-0000-000000000004"), "D", "D", _a.Id);
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

        AssignUser(TestUsers.UserA, _c.Id);
        AssignUser(TestUsers.UserB, _b.Id);
        AssignUser(TestUsers.UserC, _a.Id);
        AssignUser(TestUsers.UserD, _d.Id);
        await _context.SaveChangesAsync();

        (await EvaluateForUser(TestUsers.UserA)).Effect.ShouldBe(Effect.Allow, "user in C");
        (await EvaluateForUser(TestUsers.UserB)).Effect.ShouldBe(Effect.Allow, "user in B (C's ancestor)");
        (await EvaluateForUser(TestUsers.UserC)).Effect.ShouldBe(Effect.Allow, "user in A (C's ancestor)");
        (await EvaluateForUser(TestUsers.UserD)).Effect.ShouldBe(Effect.Deny, "user in D (sibling, not in propagation path)");
    }

    [Test]
    public async Task Position_Allow_On_C_Does_Not_Propagate_To_Sibling_D()
    {
        _context.Grants.Add(NewGrant(SubjectType.Position, _c.Id, Effect.Allow, SourceType.Position, _c.Id, SourcePriority.RoleOrRoleGroup));
        AssignUser(TestUsers.UserD, _d.Id);
        await _context.SaveChangesAsync();

        (await EvaluateForUser(TestUsers.UserD)).Effect.ShouldBe(Effect.Deny);
    }

    [Test]
    public async Task Position_Deny_On_B_Affects_User_In_B_C_But_Not_A()
    {
        _context.Grants.Add(NewGrant(SubjectType.Position, _b.Id, Effect.Deny, SourceType.Position, _b.Id, SourcePriority.RoleOrRoleGroup));
        AssignUser(TestUsers.UserA, _b.Id);
        AssignUser(TestUsers.UserB, _c.Id);
        AssignUser(TestUsers.UserC, _a.Id);
        await _context.SaveChangesAsync();

        (await EvaluateForUser(TestUsers.UserA)).Effect.ShouldBe(Effect.Deny, "user in B");
        (await EvaluateForUser(TestUsers.UserB)).Effect.ShouldBe(Effect.Deny, "user in C (B's descendant)");
        (await EvaluateForUser(TestUsers.UserC)).Effect.ShouldBe(Effect.Deny, "user in A has no applicable grant when deny is only on descendant B");
    }

    [Test]
    public async Task Position_Deny_On_B_Does_Not_Propagate_To_Ancestor_A()
    {
        _context.Grants.Add(NewUserGrant(TestUsers.UserC, Effect.Allow, SourcePriority.IndividualOverride));
        _context.Grants.Add(NewGrant(SubjectType.Position, _b.Id, Effect.Deny, SourceType.Position, _b.Id, SourcePriority.RoleOrRoleGroup));
        AssignUser(TestUsers.UserC, _a.Id);
        await _context.SaveChangesAsync();

        (await EvaluateForUser(TestUsers.UserC)).Effect.ShouldBe(Effect.Allow, "ancestor must not receive propagated deny from B");
    }

    [Test]
    public async Task Re_Parenting_C_From_B_To_D_Changes_Propagation()
    {
        _context.Grants.Add(NewGrant(SubjectType.Position, _c.Id, Effect.Allow, SourceType.Position, _c.Id, SourcePriority.RoleOrRoleGroup));
        AssignUser(TestUsers.UserB, _b.Id);
        AssignUser(TestUsers.UserD, _d.Id);
        await _context.SaveChangesAsync();

        (await EvaluateForUser(TestUsers.UserB)).Effect.ShouldBe(Effect.Allow, "user in B (C's ancestor)");

        var trackedC = await _context.Positions.SingleAsync(p => p.Id == _c.Id);
        var trackedD = await _context.Positions.SingleAsync(p => p.Id == _d.Id);
        var all = await _context.Positions.AsNoTracking().ToListAsync();
        trackedC.Reparent(trackedD, all, new PositionHierarchyService());
        await _context.SaveChangesAsync();

        (await EvaluateForUser(TestUsers.UserB)).Effect.ShouldBe(Effect.Deny, "C no longer under B; Allow no longer propagates to user in B");
        (await EvaluateForUser(TestUsers.UserD)).Effect.ShouldBe(Effect.Allow, "Allow now propagates to user in D (C's new ancestor)");
    }

    [Test]
    public async Task Individual_Allow_Does_Not_Propagate_To_Ancestor_Or_Descendant()
    {
        _context.Grants.Add(NewUserGrant(TestUsers.UserA, Effect.Allow, SourcePriority.IndividualOverride));
        _context.Grants.Add(NewGrant(SubjectType.Position, _c.Id, Effect.Deny, SourceType.Position, _c.Id, SourcePriority.RoleOrRoleGroup));
        await _context.SaveChangesAsync();

        (await EvaluateForUser(TestUsers.UserA)).Effect.ShouldBe(Effect.Allow);
    }

    [Test]
    public async Task User_Changing_Position_Does_Not_Move_Individual_Grant()
    {
        _context.Grants.Add(NewUserGrant(TestUsers.UserA, Effect.Allow, SourcePriority.IndividualOverride));
        _context.Grants.Add(NewGrant(SubjectType.Position, _c.Id, Effect.Deny, SourceType.Position, _c.Id, SourcePriority.RoleOrRoleGroup));
        _context.PositionAssignments.Add(PositionAssignment.Create(
            EnsurePersonnel(TestUsers.UserA).Id, _c.Id, T0.AddDays(-30)));
        var personnelA = EnsurePersonnel(TestUsers.UserA);
        await _context.SaveChangesAsync();

        (await EvaluateForUser(TestUsers.UserA)).Effect.ShouldBe(Effect.Allow);

        var assignment = await _context.PositionAssignments.SingleAsync(a => a.PersonnelId == personnelA.Id);
        _context.PositionAssignments.Remove(assignment);
        _context.PositionAssignments.Add(PositionAssignment.Create(
            EnsurePersonnel(TestUsers.UserA).Id, _d.Id, T0));
        await _context.SaveChangesAsync();

        (await EvaluateForUser(TestUsers.UserA)).Effect.ShouldBe(Effect.Allow,
            "individual grant must remain isolated from position propagation even after the user moves");
    }

    [Test]
    public async Task Individual_Deny_Does_Not_Propagate_To_Ancestor_Position()
    {
        _context.Grants.Add(NewUserGrant(TestUsers.UserA, Effect.Deny, SourcePriority.IndividualOverride));
        _context.Grants.Add(NewGrant(SubjectType.Position, _c.Id, Effect.Allow, SourceType.Position, _c.Id, SourcePriority.RoleOrRoleGroup));
        _context.PositionAssignments.Add(PositionAssignment.Create(
            EnsurePersonnel(TestUsers.UserA).Id, _c.Id, T0.AddDays(-30)));
        await _context.SaveChangesAsync();

        (await EvaluateForUser(TestUsers.UserA)).Effect.ShouldBe(Effect.Deny);

        _context.Grants.Add(NewUserGrant(TestUsers.UserB, Effect.Allow, SourcePriority.IndividualOverride));
        _context.PositionAssignments.Add(PositionAssignment.Create(
            EnsurePersonnel(TestUsers.UserB).Id, _b.Id, T0.AddDays(-30)));
        await _context.SaveChangesAsync();

        (await EvaluateForUser(TestUsers.UserB)).Effect.ShouldBe(Effect.Allow,
            "another user in B must not be affected by user A's individual deny");
    }

    private Task<AccessDecision> EvaluateForUser(Guid userId) =>
        _evaluator.EvaluateAsync(AccessRequest.ForUser(userId, "Read", "Personnel", null, T0));

    private void AssignUser(Guid identityUserId, Guid positionId)
    {
        var personnel = EnsurePersonnel(identityUserId);
        _context.PositionAssignments.Add(PositionAssignment.Create(
            personnel.Id, positionId, T0.AddDays(-30)));
    }

    private Personnel EnsurePersonnel(Guid identityUserId)
    {
        var existing = _context.Personnel.Local.FirstOrDefault(p => p.IdentityUserId == identityUserId)
            ?? _context.Personnel.FirstOrDefault(p => p.IdentityUserId == identityUserId);
        if (existing is not null)
        {
            return existing;
        }

        var personnel = Personnel.Create(
            $"NID-{identityUserId:N}",
            $"User{identityUserId.ToString()[..8]}",
            "Test",
            $"PC-{identityUserId.ToString()[..8]}",
            identityUserId: identityUserId);
        _context.Personnel.Add(personnel);
        return personnel;
    }

    private static Position PositionWithId(Guid id, string code, string title, Guid? parentId = null, Guid? companyId = null)
    {
        var position = Position.Create(companyId ?? TestGuids.CompanyA, code, title, parentPositionId: parentId);
        position.Id = id;
        return position;
    }

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
