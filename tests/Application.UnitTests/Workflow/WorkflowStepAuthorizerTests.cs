using qc_authorization.Application.Authorization.Evaluation;
using qc_authorization.Application.UnitTests.TestSupport;
using qc_authorization.Application.Workflow;
using qc_authorization.Domain.Authorization;
using qc_authorization.Domain.Authorization.Enums;
using qc_authorization.Domain.Authorization.ValueObjects;
using qc_authorization.Infrastructure.Data;
using NUnit.Framework;
using Shouldly;

namespace qc_authorization.Application.UnitTests.Workflow;

[TestFixture]
public class WorkflowStepAuthorizerTests
{
    private ApplicationDbContext _context = null!;
    private WorkflowStepAuthorizer _authorizer = null!;
    private static readonly DateTimeOffset T0 = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    [SetUp]
    public async Task SetUp()
    {
        AccessEvaluator evaluator;
        (_context, evaluator) = AuthorizationTestContext.Create();
        _authorizer = new WorkflowStepAuthorizer(evaluator);

        await _context.Database.EnsureCreatedAsync();
        var perm = Permission.Create("PERSONNEL.READ", "Personnel", "Read");
        _context.Permissions.Add(perm);
        _context.Grants.Add(Grant.CreateForUser(
            TestUsers.UserA,
            perm.Id,
            SourceType.User,
            0,
            Effect.Allow,
            T0.AddDays(-1),
            null,
            SourcePriority.IndividualOverride));
        await _context.SaveChangesAsync();
    }

    [TearDown]
    public async Task TearDown()
    {
        await _context.Database.EnsureDeletedAsync();
        await _context.DisposeAsync();
    }

    [Test]
    public async Task Authorized_Step_Returns_Allow()
    {
        var decision = await _authorizer.AuthorizeAsync(
            TestUsers.UserA,
            new WorkflowStepRequirement("Personnel.Read", "Personnel"),
            T0);

        decision.Effect.ShouldBe(Effect.Allow);
    }

    [Test]
    public async Task Unauthorized_Step_Returns_Deny()
    {
        var decision = await _authorizer.AuthorizeAsync(
            TestUsers.Unknown,
            new WorkflowStepRequirement("Personnel.Read", "Personnel"),
            T0);

        decision.Effect.ShouldBe(Effect.Deny);
    }

    [Test]
    public async Task Trace_Contains_Workflow_Context()
    {
        var decision = await _authorizer.AuthorizeAsync(
            TestUsers.UserA,
            new WorkflowStepRequirement("Personnel.Read", "Personnel", "r-1"),
            T0);

        decision.Trace.ResourceId.ShouldBe("r-1");
        decision.Trace.RequestedPermission.ShouldBe("Personnel.Read");
    }
}
