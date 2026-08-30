using Microsoft.Extensions.DependencyInjection;
using AccessManagement.Application.Abstractions;
using AccessManagement.Application.Authorization.Audit;
using AccessManagement.Application.Authorization.Delegation;
using AccessManagement.Application.Authorization.Evaluation;
using AccessManagement.Application.Authorization.Services;
using AccessManagement.Application.Common.Interfaces;
using AccessManagement.Application.Organization;
using AccessManagement.Application.Session;
using AccessManagement.Domain.Authorization.Services;
using AccessManagement.Domain.Organization;
using AccessManagement.Tests.TestSupport;

namespace AccessManagement.Infrastructure.IntegrationTests.TestSupport;

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
        services.AddScoped<ICatalogGrantFilter, CatalogGrantFilter>();
        services.AddScoped<IOrganizationalUnitHierarchy, OrganizationalUnitHierarchyService>();
        services.AddScoped<IPositionHierarchyQuery, PositionHierarchyQuery>();
        services.AddScoped<IScopeMatcher, ScopeMatcher>();
        services.AddScoped<IGrantResolver, GrantResolver>();
        services.AddScoped<ICandidateGrantResolver, GrantResolver>();
        services.AddScoped<IDecisionTraceWriter, NullDecisionTraceWriter>();
        services.AddScoped<IDelegationSubsetPolicy, DelegationSubsetPolicy>();
        services.AddScoped<IDelegationHierarchyPolicy, DelegationHierarchyPolicy>();
        services.AddScoped<RoleGroupGrantMaterializer>();
        services.AddScoped<RoleGrantRematerializer>();
        services.AddScoped<IAccessEvaluator, AccessEvaluator>();
        services.AddScoped<IActorAccessService, ActorAccessService>();
        services.AddScoped<ICompanyVisibilityService, CompanyVisibilityService>();
        services.AddScoped<LineManagerTargetPolicy>();
        services.AddScoped<CompanyWorkspaceService>();
        services.AddScoped<IAuthorizationAuditService, AuthorizationAuditService>();
        return services;
    }
}
