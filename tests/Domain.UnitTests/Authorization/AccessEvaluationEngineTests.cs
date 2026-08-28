using qc_authorization.Domain.Authorization;
using qc_authorization.Domain.Authorization.Enums;
using qc_authorization.Domain.Authorization.Evaluation;
using qc_authorization.Domain.Authorization.ValueObjects;
using NUnit.Framework;
using Shouldly;

namespace qc_authorization.Domain.UnitTests.Authorization;

[TestFixture]
public class AccessEvaluationEngineTests
{
    private static readonly DateTimeOffset T0 = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
    private readonly AccessEvaluationEngine _engine = new();

    [Test]
    public void No_Candidates_Returns_Deny()
    {
        var request = new AccessRequest(SubjectType.Role, 1, "Read", "Personnel", null, T0);
        var decision = _engine.Evaluate(request, []);
        decision.Effect.ShouldBe(Effect.Deny);
        decision.Reason.ShouldBe(DecisionReason.NoCandidateGrants);
    }

    [Test]
    public void Higher_Priority_Grant_Wins()
    {
        var permId = 1;
        var grants = new List<Grant>
        {
            Grant.Create(SubjectType.Role, 1, permId, SourceType.Role, 1, Effect.Allow, T0, null, 10),
            Grant.Create(SubjectType.Role, 1, permId, SourceType.Role, 1, Effect.Deny, T0, null, 100),
        };
        var decision = _engine.Evaluate(
            new AccessRequest(SubjectType.Role, 1, "Read", "Personnel", null, T0),
            grants);
        decision.Effect.ShouldBe(Effect.Deny);
    }
}
