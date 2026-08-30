using AccessManagement.Domain.Authorization.ValueObjects;
using NUnit.Framework;
using Shouldly;

namespace AccessManagement.Domain.UnitTests.Authorization.ValueObjects;

using AccessManagement.Tests.TestSupport;

[TestFixture]
public class ValidityTests
{
    private static readonly DateTimeOffset T0 = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset T1 = T0.AddDays(1);
    private static readonly DateTimeOffset T2 = T0.AddDays(7);

    [Test]
    public void Valid_Grant_IsActive_DuringItsWindow()
    {
        var v = new Validity(T0, T2);
        v.IsActiveAt(T0).ShouldBeTrue();
        v.IsActiveAt(T1).ShouldBeTrue();
        v.IsActiveAt(T2).ShouldBeTrue();
    }

    [Test]
    public void Expired_Grant_IsNotActive_After_ValidTo()
    {
        var v = new Validity(T0, T1);
        v.IsActiveAt(T1.AddSeconds(1)).ShouldBeFalse();
        v.IsActiveAt(T2).ShouldBeFalse();
    }

    [Test]
    public void NotYetValid_Grant_IsNotActive_Before_ValidFrom()
    {
        var v = new Validity(T1, T2);
        v.IsActiveAt(T0).ShouldBeFalse();
    }

    [Test]
    public void OpenEnded_Validity_IsActiveForever_AfterStart()
    {
        var v = new Validity(T0, null);
        v.IsActiveAt(T2.AddYears(10)).ShouldBeTrue();
    }

    [Test]
    public void ValidTo_Before_ValidFrom_Throws()
    {
        Should.Throw<ArgumentException>(() => new Validity(T1, T0));
    }
}
