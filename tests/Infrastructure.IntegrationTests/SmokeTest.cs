namespace qc_authorization.Infrastructure.IntegrationTests;

using qc_authorization.Tests.TestSupport;

[TestFixture]
public class SmokeTest
{
    [Test]
    public void InfrastructureAssembly_Loads()
    {
        var asm = typeof(qc_authorization.Infrastructure.Data.ApplicationDbContext).Assembly;
        Assert.That(asm, Is.Not.Null);
    }
}
