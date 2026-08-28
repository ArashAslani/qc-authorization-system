using qc_authorization.Domain.Authorization;
using qc_authorization.Domain.Authorization.Constraints;
using qc_authorization.Domain.Authorization.Enums;
using qc_authorization.Domain.Authorization.Evaluation;
using qc_authorization.Domain.Authorization.ValueObjects;
using NUnit.Framework;
using Shouldly;

namespace qc_authorization.Domain.UnitTests.Authorization;

[TestFixture]
public class ConstraintTests
{
    private static readonly DateTimeOffset T0 = new(2026, 1, 1, 10, 0, 0, TimeSpan.Zero);
    private static readonly Guid UserA = Guid.Parse("11111111-1111-1111-1111-111111111101");

    [Test]
    public void AmountConstraint_Passes_When_Under_Max()
    {
        var constraint = new AmountConstraint(1000m);
        var request = new AccessRequest(
            SubjectType.User, 0, UserA, "Approve", "Payment", null, T0,
            new Dictionary<string, object> { ["Amount"] = 500m });

        constraint.IsSatisfied(request, out _).ShouldBeTrue();
    }

    [Test]
    public void AmountConstraint_Fails_When_Over_Max()
    {
        var constraint = new AmountConstraint(1000m);
        var request = new AccessRequest(
            SubjectType.User, 0, UserA, "Approve", "Payment", null, T0,
            new Dictionary<string, object> { ["Amount"] = 1500m });

        constraint.IsSatisfied(request, out var reason).ShouldBeFalse();
        reason.ShouldBe("amount-exceeds-max");
    }

    [Test]
    public void TimeConstraint_Passes_Inside_Window()
    {
        var constraint = new TimeConstraint(new TimeOnly(9, 0), new TimeOnly(17, 0));
        var request = new AccessRequest(SubjectType.User, 0, UserA, "Read", "Personnel", null, T0);

        constraint.IsSatisfied(request, out _).ShouldBeTrue();
    }

    [Test]
    public void TimeConstraint_Fails_Outside_Window()
    {
        var constraint = new TimeConstraint(new TimeOnly(11, 0), new TimeOnly(17, 0));
        var request = new AccessRequest(SubjectType.User, 0, UserA, "Read", "Personnel", null, T0);

        constraint.IsSatisfied(request, out var reason).ShouldBeFalse();
        reason.ShouldBe("outside-time-window");
    }

    [Test]
    public void ScopeConstraint_Passes_When_Value_Matches()
    {
        var constraint = new ScopeConstraint("Branch", "B-1");
        var request = new AccessRequest(
            SubjectType.User, 0, UserA, "Read", "Personnel", null, T0,
            new Dictionary<string, object> { ["Branch"] = "B-1" });

        constraint.IsSatisfied(request, out _).ShouldBeTrue();
    }

    [Test]
    public void Engine_Rejects_Grant_When_Constraint_Fails()
    {
        var engine = new AccessEvaluationEngine();
        var grant = Grant.Create(
            SubjectType.Role, 1, 1, SourceType.Role, 1, Effect.Allow, T0.AddDays(-1), null, 10,
            constraints: [GrantConstraint.FromAmount(100m)]);

        var decision = engine.Evaluate(
            new AccessRequest(
                SubjectType.Role, 1, null, "Approve", "Payment", null, T0,
                new Dictionary<string, object> { ["Amount"] = 200m }),
            [grant]);

        decision.Effect.ShouldBe(Effect.Deny);
        decision.Reason.ShouldBe(DecisionReason.ConstraintFailed);
    }
}
