using qc_authorization.Domain.Authorization.ValueObjects;
using NUnit.Framework;
using Shouldly;

namespace qc_authorization.Domain.UnitTests.Authorization.ValueObjects;

[TestFixture]
public class ScopeTests
{
    [Test]
    public void Unbounded_Scope_Has_No_Identifier()
    {
        var s = Scope.Unbounded();
        s.Kind.ShouldBe(ScopeKind.Unbounded);
        s.Identifier.ShouldBeNull();
        s.ToString().ShouldBe("*");
    }

    [Test]
    public void Company_Scope_Requires_Identifier()
    {
        var s = Scope.Company("C-1");
        s.Kind.ShouldBe(ScopeKind.Company);
        s.Identifier.ShouldBe("C-1");
    }

    [Test]
    public void Bounded_Scope_Without_Identifier_Throws()
    {
        Should.Throw<ArgumentException>(() => new Scope(ScopeKind.Company, null));
        Should.Throw<ArgumentException>(() => new Scope(ScopeKind.Company, ""));
        Should.Throw<ArgumentException>(() => new Scope(ScopeKind.Company, "   "));
    }
}
