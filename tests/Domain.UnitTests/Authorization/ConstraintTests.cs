using AccessManagement.Domain.Authorization.Constraints;
using AccessManagement.Domain.Authorization.Evaluation;
using NUnit.Framework;
using Shouldly;

namespace AccessManagement.Domain.UnitTests.Authorization;

[TestFixture]
public class ConstraintTests
{
    private static readonly DateTimeOffset T0 = new(2026, 1, 1, 10, 0, 0, TimeSpan.Zero);
    private static readonly Guid UserA = Guid.Parse("11111111-1111-1111-1111-111111111101");

    [Test]
    public void AmountConstraint_Passes_When_Under_Max()
    {
        var constraint = new AmountConstraint(1000m);
        var request = new AccessRequest(UserA, null, "Payment.Approve", null, T0,
            new Dictionary<string, object> { ["Amount"] = 500m });

        constraint.IsSatisfied(request, out _).ShouldBeTrue();
    }

    [Test]
    public void AmountConstraint_Fails_When_Over_Max()
    {
        var constraint = new AmountConstraint(1000m);
        var request = new AccessRequest(UserA, null, "Payment.Approve", null, T0,
            new Dictionary<string, object> { ["Amount"] = 1500m });

        constraint.IsSatisfied(request, out var reason).ShouldBeFalse();
        reason.ShouldBe("amount-exceeds-max");
    }

    [Test]
    public void TimeConstraint_Passes_Inside_Window()
    {
        var constraint = new TimeConstraint(new TimeOnly(9, 0), new TimeOnly(17, 0));
        var request = new AccessRequest(UserA, null, "Personnel.Read", null, T0);
        constraint.IsSatisfied(request, out _).ShouldBeTrue();
    }

    [Test]
    public void ScopeConstraint_Passes_When_Value_Matches()
    {
        var constraint = new ScopeConstraint("Branch", "B-1");
        var request = new AccessRequest(UserA, null, "Personnel.Read", null, T0,
            new Dictionary<string, object> { ["Branch"] = "B-1" });
        constraint.IsSatisfied(request, out _).ShouldBeTrue();
    }
}
