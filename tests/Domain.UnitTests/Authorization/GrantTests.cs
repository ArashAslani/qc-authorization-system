using qc_authorization.Domain.Authorization;
using qc_authorization.Domain.Authorization.Enums;
using NUnit.Framework;
using Shouldly;

namespace qc_authorization.Domain.UnitTests.Authorization;

[TestFixture]
public class GrantTests
{
    private static readonly DateTimeOffset T0 = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    [Test]
    public void Can_Create_Grant()
    {
        var g = new Grant
        {
            SubjectType = SubjectType.User,
            SubjectId = 1,
            PermissionId = 100,
            SourceType = SourceType.User,
            SourceId = 1,
            Effect = Effect.Allow,
            ValidFrom = T0,
            Priority = 100,
        };
        g.SubjectType.ShouldBe(SubjectType.User);
        g.SubjectId.ShouldBe(1);
    }

    [Test]
    public void Grant_Has_Source_Traceability()
    {
        var g = new Grant
        {
            SubjectType = SubjectType.Role,
            SubjectId = 50,
            PermissionId = 100,
            SourceType = SourceType.Role,
            SourceId = 50,
        };
        g.SourceType.ShouldBe(SourceType.Role);
        g.SourceId.ShouldBe(50);
    }

    [Test]
    public void Grant_Allow_Effect_Defaults_To_Allow()
    {
        var g = new Grant { Effect = Effect.Allow };
        g.Effect.ShouldBe(Effect.Allow);
    }

    [Test]
    public void Grant_Deny_Effect_Is_Supported()
    {
        var g = new Grant { Effect = Effect.Deny };
        g.Effect.ShouldBe(Effect.Deny);
    }

    [Test]
    public void Grant_Stores_Priority()
    {
        var g = new Grant { Priority = 90 };
        g.Priority.ShouldBe(90);
    }

    [Test]
    public void Grant_Supports_All_SubjectTypes()
    {
        foreach (SubjectType t in Enum.GetValues(typeof(SubjectType)))
        {
            var g = new Grant { SubjectType = t };
            g.SubjectType.ShouldBe(t);
        }
    }

    [Test]
    public void Grant_Supports_All_SourceTypes_ExceptNone()
    {
        foreach (SourceType s in Enum.GetValues(typeof(SourceType)))
        {
            var g = new Grant { SourceType = s };
            g.SourceType.ShouldBe(s);
        }
    }
}
