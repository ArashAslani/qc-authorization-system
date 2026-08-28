using qc_authorization.Domain.Authorization;
using qc_authorization.Domain.Authorization.Enums;
using qc_authorization.Domain.Authorization.Exceptions;
using NUnit.Framework;
using Shouldly;

namespace qc_authorization.Domain.UnitTests.Authorization;

[TestFixture]
public class GrantTests
{
    private static readonly DateTimeOffset T0 = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
    private static readonly Guid UserA = Guid.Parse("11111111-1111-1111-1111-111111111101");

    [Test]
    public void Can_Create_Grant()
    {
        var g = Grant.CreateForUser(
            UserA,
            100,
            SourceType.User,
            0,
            Effect.Allow,
            T0,
            null,
            100);

        g.SubjectType.ShouldBe(SubjectType.User);
        g.SubjectId.ShouldBe(0);
        g.SubjectUserId.ShouldBe(UserA);
    }

    [Test]
    public void Grant_Has_Source_Traceability()
    {
        var g = Grant.Create(
            SubjectType.Role,
            50,
            100,
            SourceType.Role,
            50,
            Effect.Allow,
            T0,
            null,
            50);

        g.SourceType.ShouldBe(SourceType.Role);
        g.SourceId.ShouldBe(50);
    }

    [Test]
    public void Grant_Allow_Effect_Is_Preserved()
    {
        var g = Grant.CreateForUser(
            UserA,
            1,
            SourceType.User,
            0,
            Effect.Allow,
            T0,
            null,
            1);

        g.Effect.ShouldBe(Effect.Allow);
    }

    [Test]
    public void Grant_Deny_Effect_Is_Supported()
    {
        var g = Grant.CreateForUser(
            UserA,
            1,
            SourceType.User,
            0,
            Effect.Deny,
            T0,
            null,
            1);

        g.Effect.ShouldBe(Effect.Deny);
    }

    [Test]
    public void Grant_Stores_Priority()
    {
        var g = Grant.CreateForUser(
            UserA,
            1,
            SourceType.User,
            0,
            Effect.Allow,
            T0,
            null,
            90);

        g.Priority.ShouldBe(90);
    }

    [Test]
    public void Grant_Supports_All_SubjectTypes()
    {
        foreach (SubjectType t in Enum.GetValues(typeof(SubjectType)))
        {
            var g = t == SubjectType.User
                ? Grant.CreateForUser(UserA, 1, SourceType.User, 0, Effect.Allow, T0, null, 1)
                : Grant.Create(t, 1, 1, SourceType.Role, 1, Effect.Allow, T0, null, 1);
            g.SubjectType.ShouldBe(t);
        }
    }

    [Test]
    public void Grant_Supports_All_SourceTypes()
    {
        foreach (SourceType s in Enum.GetValues(typeof(SourceType)))
        {
            var g = s == SourceType.User
                ? Grant.CreateForUser(UserA, 1, s, 0, Effect.Allow, T0, null, 1)
                : Grant.Create(SubjectType.User, 0, 1, s, 1, Effect.Allow, T0, null, 1, subjectUserId: UserA);
            g.SourceType.ShouldBe(s);
        }
    }

    [Test]
    public void Grant_Rejects_Invalid_SourceId()
    {
        Should.Throw<AuthorizationDomainException>(() =>
            Grant.Create(SubjectType.User, 0, 1, SourceType.User, 0, Effect.Allow, T0, null, 1, subjectUserId: UserA));
    }
}
