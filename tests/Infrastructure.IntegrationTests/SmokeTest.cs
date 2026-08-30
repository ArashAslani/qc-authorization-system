namespace AccessManagement.Infrastructure.IntegrationTests;

using AccessManagement.Tests.TestSupport;

[TestFixture]
public class SmokeTest
{
    [Test]
    public void InfrastructureAssembly_Loads()
    {
        var asm = typeof(AccessManagement.Infrastructure.Data.ApplicationDbContext).Assembly;
        Assert.That(asm, Is.Not.Null);
    }
}
