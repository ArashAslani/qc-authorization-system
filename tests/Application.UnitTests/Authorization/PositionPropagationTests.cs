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
[NonParallelizable]
public class PositionPropagationTests
{
    private ApplicationDbContext _context = null!;
    private AccessEvaluator _evaluator = null!;
    private Permission _perm = null!;
    private static readonly DateTimeOffset T0 = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    // Tree:
    //   A (id 1)
    //   ├── B (id 2)
    //   │   └── C (id 3)
    //   └── D (id 4)
    private Position _a = null!;
    private Position _b = null!;
    private Position _c = null!;
    private Position _d = null!;

    [SetUp]
    public async Task SetUp()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"qc-prop-{Guid.NewGuid():N}")
            .EnableServiceProviderCaching(false)
            .Options;
        _context = new ApplicationDbContext(options);
        await _context.Database.EnsureCreatedAsync();

        _a = new Position { Id = 1, Code = "A", Name = "A" };
        _b = new Position { Id = 2, Code = "B", Name = "B", ParentId = 1 };
        _c = new Position { Id = 3, Code = "C", Name = "C", ParentId = 2 };
        _d = new Position { Id = 4, Code = "D", Name = "D", ParentId = 1 };
        _context.Positions.AddRange(_a, _b, _c, _d);

        _perm = new Permission { Code = "PERSONNEL.READ", Resource = "Personnel", Action = "Read" };
        _context.Permissions.Add(_perm);
        await _context.SaveChangesAsync();

        var hierarchy = new PositionHierarchyService();
        _evaluator = new AccessEvaluator(new PositionAwareCandidateGrantResolver(_context, hierarchy));
    }

    [TearDown]
    public async Task TearDown()
    {
        await _context.Database.EnsureDeletedAsync();
        await _context.DisposeAsync();
    }

    // --- Position grant propagation (Grant direction: P + Ancestors(P)) ---

    [Test]
    public async Task Position_Allow_On_C_Affects_User_In_C_B_And_A()
    {
        _context.Grants.Add(NewGrant(SubjectType.Position, _c.Id, Effect.Allow, SourceType.Position, _c.Id, SourcePriority.RoleOrRoleGroup));
        await _context.SaveChangesAsync();

        // User in C.
        AssignUser(80, _c.Id);
        // User in B.
        AssignUser(81, _b.Id);
        // User in A.
        AssignUser(82, _a.Id);
        // User in D (sibling, not in propagation path).
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

    // --- Position revoke propagation (Revoke direction: P + Descendants(P)) ---

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
        (await EvaluateForUser(82)).Effect.ShouldBe(Effect.Deny, "user in A: ancestor must NOT be affected by a Deny propagation");
    }

    // --- Hierarchy changes ---

    [Test]
    public async Task Re_Parenting_C_From_B_To_D_Changes_Propagation()
    {
        // C is initially under B; granting Allow on C propagates to B, A.
        _context.Grants.Add(NewGrant(SubjectType.Position, _c.Id, Effect.Allow, SourceType.Position, _c.Id, SourcePriority.RoleOrRoleGroup));
        AssignUser(81, _b.Id);
        AssignUser(83, _d.Id);
        await _context.SaveChangesAsync();

        (await EvaluateForUser(81)).Effect.ShouldBe(Effect.Allow, "user in B (C's ancestor)");

        // Re-parent C under D.
        var trackedC = await _context.Positions.SingleAsync(p => p.Id == _c.Id);
        trackedC.ParentId = _d.Id;
        await _context.SaveChangesAsync();

        (await EvaluateForUser(81)).Effect.ShouldBe(Effect.Deny, "C no longer under B; Allow no longer propagates to user in B");
        (await EvaluateForUser(83)).Effect.ShouldBe(Effect.Allow, "Allow now propagates to user in D (C's new ancestor)");
    }

    // --- Individual grant isolation ---

    [Test]
    public async Task Individual_Allow_Does_Not_Propagate_To_Ancestor_Or_Descendant()
    {
        // User 80 holds an Individual Allow.
        _context.Grants.Add(NewGrant(SubjectType.User, 80, Effect.Allow, SourceType.User, 80, SourcePriority.IndividualOverride));
        // Grant from C, B, A positions to allow propagation to be visible.
        _context.Grants.Add(NewGrant(SubjectType.Position, _c.Id, Effect.Deny, SourceType.Position, _c.Id, SourcePriority.RoleOrRoleGroup));
        await _context.SaveChangesAsync();

        // User 80, not assigned to any position, gets the individual grant.
        (await EvaluateForUser(80)).Effect.ShouldBe(Effect.Allow);
    }

    [Test]
    public async Task User_Changing_Position_Does_Not_Move_Individual_Grant()
    {
        // User 80 has an Individual Allow and is in C (which has a Deny).
        _context.Grants.Add(NewGrant(SubjectType.User, 80, Effect.Allow, SourceType.User, 80, SourcePriority.IndividualOverride));
        _context.Grants.Add(NewGrant(SubjectType.Position, _c.Id, Effect.Deny, SourceType.Position, _c.Id, SourcePriority.RoleOrRoleGroup));
        _context.PositionAssignments.Add(new PositionAssignment
        {
            PersonnelId = 80,
            PositionId = _c.Id,
            ValidFrom = T0.AddDays(-30),
        });
        await _context.SaveChangesAsync();

        // In C: Position Deny (with propagation to B, A) vs Individual Allow.
        // Individual Override > Position Override => Allow.
        (await EvaluateForUser(80)).Effect.ShouldBe(Effect.Allow);

        // Move user 80 to D.
        var assignment = await _context.PositionAssignments.SingleAsync(a => a.PersonnelId == 80);
        assignment.PositionId = _d.Id;
        await _context.SaveChangesAsync();

        // Individual Allow is bound to the user, NOT to a position. So
        // it still applies. The user is in D, which has no Position Deny
        // for them; so the result remains Allow.
        (await EvaluateForUser(80)).Effect.ShouldBe(Effect.Allow,
            "individual grant must remain isolated from position propagation even after the user moves");
    }

    [Test]
    public async Task Individual_Deny_Does_Not_Propagate_To_Ancestor_Position()
    {
        // User 80 in C. Individual Deny. Position C has Allow.
        _context.Grants.Add(NewGrant(SubjectType.User, 80, Effect.Deny, SourceType.User, 80, SourcePriority.IndividualOverride));
        _context.Grants.Add(NewGrant(SubjectType.Position, _c.Id, Effect.Allow, SourceType.Position, _c.Id, SourcePriority.RoleOrRoleGroup));
        _context.PositionAssignments.Add(new PositionAssignment
        {
            PersonnelId = 80,
            PositionId = _c.Id,
            ValidFrom = T0.AddDays(-30),
        });
        await _context.SaveChangesAsync();

        // Individual Deny wins.
        (await EvaluateForUser(80)).Effect.ShouldBe(Effect.Deny);

        // The Deny must NOT propagate to the user's ancestor positions.
        // B is the user's parent position; querying for a User in B
        // without Individual must NOT see the Deny.
        _context.Grants.Add(new Grant
        {
            SubjectType = SubjectType.User,
            SubjectId = 81, // a different user, also in B
            Effect = Effect.Allow,
            SourceType = SourceType.User,
            SourceId = 81,
            PermissionId = _perm.Id,
            ValidFrom = T0.AddDays(-1),
            Priority = SourcePriority.IndividualOverride,
        });
        _context.PositionAssignments.Add(new PositionAssignment
        {
            PersonnelId = 81,
            PositionId = _b.Id,
            ValidFrom = T0.AddDays(-30),
        });
        await _context.SaveChangesAsync();

        (await EvaluateForUser(81)).Effect.ShouldBe(Effect.Allow,
            "another user in B must not be affected by user 80's individual deny");
    }

    private Task<AccessDecision> EvaluateForUser(int userId) =>
        _evaluator.EvaluateAsync(new AccessRequest(SubjectType.User, userId, "Read", "Personnel", null, T0));

    private void AssignUser(int userId, int positionId) =>
        _context.PositionAssignments.Add(new PositionAssignment
        {
            PersonnelId = userId,
            PositionId = positionId,
            ValidFrom = T0.AddDays(-30),
        });

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
