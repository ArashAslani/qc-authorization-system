using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using AccessManagement.Application.Abstractions;
using AccessManagement.Application.Authorization.Audit;
using AccessManagement.Application.Authorization.Commands.CreateGrant;
using AccessManagement.Application.Authorization.Delegation;
using AccessManagement.Application.Authorization.Evaluation;
using AccessManagement.Application.Authorization.Services;
using AccessManagement.Application.Common.Interfaces;
using AccessManagement.Application.Common.Mappings;
using AccessManagement.Application.Organization;
using AccessManagement.Application.Session;
using AccessManagement.Domain.Authorization.Services;
using AccessManagement.Domain.Organization;
using AccessManagement.Infrastructure.Data;
using AccessManagement.Tests.TestSupport;

namespace AccessManagement.Application.UnitTests.TestSupport;

internal static class AuthorizationTestContext
{
    public static (ApplicationDbContext Context, AccessEvaluator Evaluator) Create(Guid? activeCompanyId = null)
    {
        var companyId = activeCompanyId ?? TestGuids.CompanyA;
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"access-{Guid.NewGuid():N}")
            .EnableServiceProviderCaching(false)
            .Options;

        var context = new ApplicationDbContext(options);
        SeedCompany(context, companyId);

        var hierarchy = new PositionHierarchyService();
        var applicability = new GrantApplicabilityService(hierarchy);
        var catalogFilter = new CatalogGrantFilter(context);
        var resolver = new GrantResolver(context, applicability, catalogFilter);
        var units = new OrganizationalUnitHierarchyService(context);
        var evaluator = new AccessEvaluator(resolver, new ScopeMatcher(units), new NullDecisionTraceWriter());

        return (context, evaluator);
    }

    public static void SeedCompany(ApplicationDbContext context, Guid companyUnitId, string name = "Company")
    {
        if (context.OrganizationalUnits.Any(u => u.Id == companyUnitId))
        {
            return;
        }

        var unit = OrganizationalUnit.Create(OrganizationalUnitTypes.Company, name);
        unit.Id = companyUnitId;
        context.OrganizationalUnits.Add(unit);
        context.SaveChanges();
    }

    public static IServiceProvider CreateMediatorServices(ApplicationDbContext context, Guid? activeCompanyId = null)
    {
        var companyId = activeCompanyId ?? TestGuids.CompanyA;
        MappingConfig.RegisterMappings();

        var hierarchy = new PositionHierarchyService();
        var applicability = new GrantApplicabilityService(hierarchy);

        return new ServiceCollection()
            .AddLogging()
            .AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<CreateGrantCommand>())
            .AddSingleton(hierarchy)
            .AddSingleton(applicability)
            .AddSingleton<ICurrentUser>(new StaticCurrentUser(activeCompanyId: companyId))
            .AddScoped<IApplicationDbContext>(_ => context)
            .AddScoped<IAuthorizationAuditService, AuthorizationAuditService>()
            .AddScoped<ICatalogGrantFilter, CatalogGrantFilter>()
            .AddScoped<IOrganizationalUnitHierarchy, OrganizationalUnitHierarchyService>()
            .AddScoped<IPositionHierarchyQuery, PositionHierarchyQuery>()
            .AddScoped<IScopeMatcher, ScopeMatcher>()
            .AddScoped<IGrantResolver, GrantResolver>()
            .AddScoped<ICandidateGrantResolver, GrantResolver>()
            .AddScoped<IDecisionTraceWriter, NullDecisionTraceWriter>()
            .AddScoped<IDelegationHierarchyPolicy, DelegationHierarchyPolicy>()
            .AddScoped<RoleGroupGrantMaterializer>()
            .AddScoped<IAccessEvaluator, AccessEvaluator>()
            .AddScoped<IDelegationSubsetPolicy, DelegationSubsetPolicy>()
            .AddScoped<CompanyWorkspaceService>()
            .BuildServiceProvider();
    }
}
