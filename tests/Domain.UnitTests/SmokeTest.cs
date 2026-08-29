namespace qc_authorization.Domain.UnitTests;

using qc_authorization.Tests.TestSupport;

[TestFixture]
public class SmokeTest
{
    [Test]
    public void DomainAssembly_Loads()
    {
        var asm = typeof(qc_authorization.Domain.Common.BaseEntity).Assembly;
        Assert.That(asm, Is.Not.Null);
    }
}
