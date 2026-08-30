namespace AccessManagement.Domain.UnitTests;

using AccessManagement.Tests.TestSupport;

[TestFixture]
public class SmokeTest
{
    [Test]
    public void DomainAssembly_Loads()
    {
        var asm = typeof(AccessManagement.Domain.Common.BaseEntity).Assembly;
        Assert.That(asm, Is.Not.Null);
    }
}
