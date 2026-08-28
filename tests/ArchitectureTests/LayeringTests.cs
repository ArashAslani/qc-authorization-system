using NetArchTest.Rules;

namespace qc_authorization.ArchitectureTests;

[TestFixture]
public class LayeringTests
{
    private const string ApplicationNamespace = "qc_authorization.Application";
    private const string DomainNamespace = "qc_authorization.Domain";
    private const string InfrastructureNamespace = "qc_authorization.Infrastructure";
    private const string WebNamespace = "qc_authorization.Web";

    [Test]
    public void Domain_ShouldNotDependOnInfrastructureOrWeb()
    {
        var result = Types.InAssembly(typeof(Domain.Common.BaseEntity).Assembly)
            .ShouldNot()
            .HaveDependencyOnAny(InfrastructureNamespace, WebNamespace, "Microsoft.EntityFrameworkCore", "Microsoft.AspNetCore")
            .GetResult();

        Assert.That(result.IsSuccessful, Is.True,
            "Domain layer leaked forbidden dependencies: " + string.Join(", ", result.FailingTypeNames ?? Array.Empty<string>()));
    }

    [Test]
    public void Domain_ShouldNotDependOnApplication()
    {
        var result = Types.InAssembly(typeof(Domain.Common.BaseEntity).Assembly)
            .ShouldNot()
            .HaveDependencyOnAny(ApplicationNamespace)
            .GetResult();

        Assert.That(result.IsSuccessful, Is.True,
            "Domain layer must not reference Application: " + string.Join(", ", result.FailingTypeNames ?? Array.Empty<string>()));
    }

    [Test]
    public void Application_ShouldNotDependOnInfrastructureOrWeb()
    {
        var result = Types.InAssembly(typeof(Application.Common.Interfaces.IUser).Assembly)
            .ShouldNot()
            .HaveDependencyOnAny(InfrastructureNamespace, WebNamespace)
            .GetResult();

        Assert.That(result.IsSuccessful, Is.True,
            "Application layer leaked forbidden dependencies: " + string.Join(", ", result.FailingTypeNames ?? Array.Empty<string>()));
    }

    [Test]
    public void Application_ShouldNotReferenceEntityFrameworkCore()
    {
        var asm = typeof(Application.Common.Interfaces.IUser).Assembly;
        var refs = asm.GetReferencedAssemblies().Select(a => a.Name ?? string.Empty);
        Assert.That(refs.Any(r => r.StartsWith("Microsoft.EntityFrameworkCore")), Is.False,
            "Application must not reference Microsoft.EntityFrameworkCore.*");
    }

    [Test]
    public void Infrastructure_MayDependOnDomainAndApplication_ButNotWeb()
    {
        var result = Types.InAssembly(typeof(Infrastructure.Data.ApplicationDbContext).Assembly)
            .ShouldNot()
            .HaveDependencyOnAny(WebNamespace)
            .GetResult();

        Assert.That(result.IsSuccessful, Is.True,
            "Infrastructure must not depend on Web: " + string.Join(", ", result.FailingTypeNames ?? Array.Empty<string>()));
    }

    [Test]
    public void Domain_ShouldNotReferenceEntityFrameworkCore()
    {
        var asm = typeof(Domain.Common.BaseEntity).Assembly;
        var refs = asm.GetReferencedAssemblies().Select(a => a.Name ?? string.Empty);
        Assert.That(refs.Any(r => r.StartsWith("Microsoft.EntityFrameworkCore")), Is.False,
            "Domain must not reference Microsoft.EntityFrameworkCore.*");
    }
}
