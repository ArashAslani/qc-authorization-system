using Microsoft.Extensions.DependencyInjection;
using qc_authorization.Application.Authorization.Audit;
using qc_authorization.Application.Authorization.Delegation;
using qc_authorization.Application.Authorization.Evaluation;
using qc_authorization.Application.Authorization.Services;
using qc_authorization.Application.Common.Interfaces;
using qc_authorization.Domain.Authorization.Evaluation;
using qc_authorization.Domain.Authorization.Services;
using qc_authorization.Domain.Organization;
using qc_authorization.Tests.TestSupport;

namespace qc_authorization.Infrastructure.IntegrationTests.TestSupport;

public static class TestServiceCollectionExtensions
{
    public static IServiceCollection AddTestCurrentUser(
        this IServiceCollection services,
        Guid? activeCompanyId = null,
        Guid? userId = null,
        Guid? personnelId = null) =>
        services.AddSingleton<ICurrentUser>(new StaticCurrentUser(userId, personnelId, activeCompanyId ?? TestGuids.CompanyA));

    public static IServiceCollection AddAuthorizationEvaluationServices(this IServiceCollection services)
    {
        services.AddSingleton<PositionHierarchyService>();
        services.AddSingleton<GrantApplicabilityService>();
        services.AddSingleton<AccessEvaluationEngine>();
        services.AddScoped<ICatalogGrantFilter, CatalogGrantFilter>();
        services.AddScoped<IDelegationSubsetPolicy, DelegationSubsetPolicy>();
        services.AddScoped<IDelegationHierarchyPolicy, DelegationHierarchyPolicy>();
        services.AddScoped<RoleGroupGrantMaterializer>();
        services.AddScoped<ICandidateGrantResolver, PositionAwareCandidateGrantResolver>();
        services.AddScoped<IAccessEvaluator, AccessEvaluator>();
        services.AddScoped<IAuthorizationAuditService, AuthorizationAuditService>();
        return services;
    }
}
