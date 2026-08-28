using System.Reflection;
using qc_authorization.Application.Authorization.Audit;
using qc_authorization.Application.Authorization.Delegation;
using qc_authorization.Application.Authorization.Evaluation;
using qc_authorization.Application.Workflow;
using qc_authorization.Application.Common.Behaviours;
using qc_authorization.Domain.Authorization.Evaluation;
using qc_authorization.Domain.Authorization.Services;
using qc_authorization.Domain.Organization;
using Microsoft.Extensions.Hosting;

namespace Microsoft.Extensions.DependencyInjection;

public static class DependencyInjection
{
    public static void AddApplicationServices(this IHostApplicationBuilder builder)
    {
        builder.Services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

        builder.Services.AddSingleton<PositionHierarchyService>();
        builder.Services.AddSingleton<GrantApplicabilityService>();
        builder.Services.AddSingleton<AccessEvaluationEngine>();

        builder.Services.AddScoped<ICandidateGrantResolver, PositionAwareCandidateGrantResolver>();
        builder.Services.AddScoped<IAccessEvaluator, AccessEvaluator>();
        builder.Services.AddScoped<IDelegationSubsetPolicy, DelegationSubsetPolicy>();
        builder.Services.AddScoped<IAuthorizationAuditService, AuthorizationAuditService>();
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
