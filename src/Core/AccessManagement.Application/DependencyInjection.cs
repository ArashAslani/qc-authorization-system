using System.Reflection;
using AccessManagement.Application.Abstractions;
using AccessManagement.Application.Authorization;
using AccessManagement.Application.Authorization.Audit;
using AccessManagement.Application.Authorization.Delegation;
using AccessManagement.Application.Authorization.Evaluation;
using AccessManagement.Application.Authorization.Services;
using AccessManagement.Application.Common.Behaviours;
using AccessManagement.Application.Common.Mappings;
using AccessManagement.Application.Organization;
using AccessManagement.Application.Session;
using AccessManagement.Application.Workflow;
using AccessManagement.Domain.Authorization.Services;
using AccessManagement.Domain.Organization;
using Microsoft.Extensions.Hosting;

namespace Microsoft.Extensions.DependencyInjection;

public static class DependencyInjection
{
    public static IHostApplicationBuilder AddAccessManagementCore(this IHostApplicationBuilder builder)
    {
        builder.AddApplicationServices();
        return builder;
    }

    public static IServiceCollection AddAccessPlugin<TSeeder>(this IServiceCollection services)
        where TSeeder : class, IAccessPluginSeeder
    {
        services.AddScoped<IAccessPluginSeeder, TSeeder>();
        return services;
    }

    public static void AddApplicationServices(this IHostApplicationBuilder builder)
    {
        MappingConfig.RegisterMappings();

        builder.Services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

        builder.Services.AddSingleton<PositionHierarchyService>();
        builder.Services.AddSingleton<GrantApplicabilityService>();

        builder.Services.AddScoped<IOrganizationalUnitHierarchy, OrganizationalUnitHierarchyService>();
        builder.Services.AddScoped<IPositionHierarchyQuery, PositionHierarchyQuery>();
        builder.Services.AddScoped<IScopeMatcher, ScopeMatcher>();
        builder.Services.AddScoped<IGrantResolver, GrantResolver>();
        builder.Services.AddScoped<ICandidateGrantResolver, GrantResolver>();
        builder.Services.AddScoped<IDecisionTraceWriter, DecisionTraceWriter>();
        builder.Services.AddScoped<IAccessEvaluator, AccessEvaluator>();
        builder.Services.AddScoped<ICatalogGrantFilter, CatalogGrantFilter>();
        builder.Services.AddScoped<IDelegationSubsetPolicy, DelegationSubsetPolicy>();
        builder.Services.AddScoped<IDelegationHierarchyPolicy, DelegationHierarchyPolicy>();
        builder.Services.AddScoped<RoleGroupGrantMaterializer>();
        builder.Services.AddScoped<IAuthorizationAuditService, AuthorizationAuditService>();
        builder.Services.AddScoped<CompanyWorkspaceService>();
        builder.Services.AddScoped<IActorAccessService, ActorAccessService>();
        builder.Services.AddScoped<LineManagerTargetPolicy>();
        builder.Services.AddScoped<IAccessPluginSeeder, CoreAccessSeeder>();
        builder.Services.AddScoped<WorkflowStepAuthorizer>();

        builder.Services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly());
            cfg.AddOpenRequestPreProcessor(typeof(LoggingBehaviour<>));
            cfg.AddOpenBehavior(typeof(UnhandledExceptionBehaviour<,>));
            cfg.AddOpenBehavior(typeof(AuthorizationBehaviour<,>));
            cfg.AddOpenBehavior(typeof(ValidationBehaviour<,>));
            cfg.AddOpenBehavior(typeof(PerformanceBehaviour<,>));
        });
    }
}
