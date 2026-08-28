using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using qc_authorization.Application.Authorization.Commands.CreateGrant;
using qc_authorization.Application.Authorization.Evaluation;
using qc_authorization.Application.Common.Interfaces;
using qc_authorization.Application.Common.Interfaces.Repositories;
using qc_authorization.Domain.Authorization.Evaluation;
using qc_authorization.Domain.Authorization.Services;
using qc_authorization.Domain.Organization;
using qc_authorization.Infrastructure.Data;
using qc_authorization.Infrastructure.Data.Repositories;

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
            new PositionAwareCandidateGrantResolver(
                new PermissionRepository(context),
                new GrantRepository(context),
                new PositionRepository(context),
                new PositionAssignmentRepository(context),
                new DelegationRepository(context),
                applicability),
            engine);

        return (context, evaluator);
    }

    public static IServiceProvider CreateMediatorServices(ApplicationDbContext context)
    {
        return new ServiceCollection()
            .AddLogging()
            .AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<CreateGrantCommand>())
            .AddScoped<IUnitOfWork>(_ => new UnitOfWork(context))
            .AddScoped<IGrantRepository>(_ => new GrantRepository(context))
            .AddScoped<IPermissionRepository>(_ => new PermissionRepository(context))
            .AddScoped<IPositionRepository>(_ => new PositionRepository(context))
            .AddScoped<IPositionAssignmentRepository>(_ => new PositionAssignmentRepository(context))
            .AddScoped<IDelegationRepository>(_ => new DelegationRepository(context))
            .BuildServiceProvider();
    }
}
