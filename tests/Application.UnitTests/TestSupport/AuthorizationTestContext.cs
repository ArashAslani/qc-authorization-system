using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using qc_authorization.Application.Authorization.Audit;
using qc_authorization.Application.Authorization.Commands.CreateGrant;
using qc_authorization.Application.Authorization.Delegation;
using qc_authorization.Application.Authorization.Evaluation;
using qc_authorization.Application.Common.Interfaces;
using qc_authorization.Domain.Authorization.Evaluation;
using qc_authorization.Domain.Authorization.Services;
using qc_authorization.Domain.Organization;
using qc_authorization.Infrastructure.Data;

namespace qc_authorization.Application.UnitTests.TestSupport;

/// <summary>
/// Builds the authorization evaluation stack backed by an in-memory database.
/// </summary>
internal static class AuthorizationTestContext
{
    public static (ApplicationDbContext Context, AccessEvaluator Evaluator) Create()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"qc-auth-{Guid.NewGuid():N}")
            .EnableServiceProviderCaching(false)
            .Options;

        var context = new ApplicationDbContext(options);
        var hierarchy = new PositionHierarchyService();
        var applicability = new GrantApplicabilityService(hierarchy);
        var engine = new AccessEvaluationEngine();

        var evaluator = new AccessEvaluator(
            new PositionAwareCandidateGrantResolver(context, applicability),
            engine);

        return (context, evaluator);
    }

    public static IServiceProvider CreateMediatorServices(ApplicationDbContext context)
    {
        var hierarchy = new PositionHierarchyService();
        var applicability = new GrantApplicabilityService(hierarchy);
        var engine = new AccessEvaluationEngine();

        return new ServiceCollection()
            .AddLogging()
            .AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<CreateGrantCommand>())
            .AddSingleton(hierarchy)
            .AddSingleton(applicability)
            .AddSingleton(engine)
            .AddScoped<IApplicationDbContext>(_ => context)
            .AddScoped<IAuthorizationAuditService, AuthorizationAuditService>()
            .AddScoped<ICandidateGrantResolver, PositionAwareCandidateGrantResolver>()
            .AddScoped<IAccessEvaluator, AccessEvaluator>()
            .AddScoped<IDelegationSubsetPolicy, DelegationSubsetPolicy>()
            .BuildServiceProvider();
    }
}
