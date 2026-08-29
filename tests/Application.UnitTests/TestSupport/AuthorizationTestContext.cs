using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using qc_authorization.Application.Authorization.Audit;
using qc_authorization.Application.Authorization.Commands.CreateGrant;
using qc_authorization.Application.Authorization.Delegation;
using qc_authorization.Application.Authorization.Evaluation;
using qc_authorization.Application.Authorization.Services;
using qc_authorization.Application.Common.Interfaces;
using qc_authorization.Application.Common.Mappings;
using qc_authorization.Domain.Authorization.Evaluation;
using qc_authorization.Domain.Authorization.Services;
using qc_authorization.Domain.Organization;
using qc_authorization.Infrastructure.Data;
using qc_authorization.Tests.TestSupport;

namespace qc_authorization.Application.UnitTests.TestSupport;

/// <summary>
/// Builds the authorization evaluation stack backed by an in-memory database.
/// </summary>
internal static class AuthorizationTestContext
{
    public static (ApplicationDbContext Context, AccessEvaluator Evaluator) Create(Guid? activeCompanyId = null)
    {
        var companyId = activeCompanyId ?? TestGuids.CompanyA;
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"qc-auth-{Guid.NewGuid():N}")
            .EnableServiceProviderCaching(false)
            .Options;

        var context = new ApplicationDbContext(options);
        var hierarchy = new PositionHierarchyService();
        var applicability = new GrantApplicabilityService(hierarchy);
        var engine = new AccessEvaluationEngine();
        var currentUser = new StaticCurrentUser(activeCompanyId: companyId);

        var catalogFilter = new CatalogGrantFilter(context);

        var evaluator = new AccessEvaluator(
            new PositionAwareCandidateGrantResolver(context, applicability, catalogFilter, currentUser),
            engine);

        return (context, evaluator);
    }

    public static IServiceProvider CreateMediatorServices(ApplicationDbContext context, Guid? activeCompanyId = null)
    {
        var companyId = activeCompanyId ?? TestGuids.CompanyA;
        MappingConfig.RegisterMappings();

        var hierarchy = new PositionHierarchyService();
        var applicability = new GrantApplicabilityService(hierarchy);
        var engine = new AccessEvaluationEngine();

        return new ServiceCollection()
            .AddLogging()
            .AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<CreateGrantCommand>())
            .AddSingleton(hierarchy)
            .AddSingleton(applicability)
            .AddSingleton(engine)
            .AddSingleton<ICurrentUser>(new StaticCurrentUser(activeCompanyId: companyId))
            .AddScoped<IApplicationDbContext>(_ => context)
            .AddScoped<IAuthorizationAuditService, AuthorizationAuditService>()
            .AddScoped<ICatalogGrantFilter, CatalogGrantFilter>()
            .AddScoped<IDelegationHierarchyPolicy, DelegationHierarchyPolicy>()
            .AddScoped<RoleGroupGrantMaterializer>()
            .AddScoped<ICandidateGrantResolver, PositionAwareCandidateGrantResolver>()
            .AddScoped<IAccessEvaluator, AccessEvaluator>()
            .AddScoped<IDelegationSubsetPolicy, DelegationSubsetPolicy>()
            .BuildServiceProvider();
    }
}
