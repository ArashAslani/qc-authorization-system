using AccessManagement.Application.Authorization.Evaluation;
using AccessManagement.Application.UnitTests.TestSupport;
using AccessManagement.Domain.Authorization;
using AccessManagement.Domain.Authorization.Enums;
using AccessManagement.Domain.Authorization.Evaluation;
using AccessManagement.Domain.Authorization.ValueObjects;
using AccessManagement.Domain.Organization;
using AccessManagement.Domain.Organization.Enums;
using AccessManagement.Infrastructure.Data;
using AccessManagement.Tests.TestSupport;
using NUnit.Framework;
using Shouldly;

namespace AccessManagement.Application.UnitTests.Authorization;

[TestFixture]
public class InactiveStatusEvaluationTests
{
    private ApplicationDbContext _db = null!;
    private AccessEvaluator _evaluator = null!;
    private Permission _perm = null!;
    private static readonly DateTimeOffset T0 = DateTimeOffset.UtcNow.AddDays(-1);

    [SetUp]
    public async Task SetUp()
    {
        (_db, _evaluator) = AuthorizationTestContext.Create();
        await _db.Database.EnsureCreatedAsync();
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
    public async Task Inactive_Personnel_Is_Denied_Despite_Grant()
    {
        var personnel = Personnel.Create("111", "A", "B", "P1", identityUserId: TestUsers.UserA, status: PersonnelStatus.Inactive);
        var position = Position.Create(TestGuids.CompanyA, "P", "Pos");
        _db.Personnel.Add(personnel);
        _db.Positions.Add(position);
        _db.PositionAssignments.Add(PositionAssignment.Create(personnel.Id, position.Id, T0));
        _db.Grants.Add(Grant.CreateForUser(
            TestUsers.UserA, _perm.Id, SourceType.User, Guid.Empty, Effect.Allow, T0, null,
            SourcePriority.IndividualOverride));
        await _db.SaveChangesAsync();

        var decision = await _evaluator.EvaluateAsync(
            new AccessRequest(TestUsers.UserA, position.Id, "RESOURCE.READ", TestGuids.CompanyA, DateTimeOffset.UtcNow));
        decision.Allowed.ShouldBeFalse();
    }

    [Test]
    public async Task Inactive_Position_Grant_Is_Ignored()
    {
        var personnel = Personnel.Create("111", "A", "B", "P1", identityUserId: TestUsers.UserA);
        var position = Position.Create(TestGuids.CompanyA, "P", "Pos", status: PositionStatus.Inactive);
        _db.Personnel.Add(personnel);
        _db.Positions.Add(position);
        _db.PositionAssignments.Add(PositionAssignment.Create(personnel.Id, position.Id, T0));
        _db.Grants.Add(Grant.Create(
            SubjectType.Position,
            position.Id,
            _perm.Id,
            SourceType.Position,
            position.Id,
            Effect.Allow,
            T0,
            null,
            SourcePriority.PositionOverride,
            scopeUnitId: TestGuids.CompanyA));
        await _db.SaveChangesAsync();

        var decision = await _evaluator.EvaluateAsync(
            new AccessRequest(TestUsers.UserA, position.Id, "RESOURCE.READ", TestGuids.CompanyA, DateTimeOffset.UtcNow));
        decision.Allowed.ShouldBeFalse();
    }
}
