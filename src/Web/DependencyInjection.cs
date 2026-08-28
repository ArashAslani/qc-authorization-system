using qc_authorization.Infrastructure.Data;
using qc_authorization.Infrastructure.Identity;
using qc_authorization.Web.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace Microsoft.Extensions.DependencyInjection;

public static class DependencyInjection
{
    public static void AddWebServices(this IHostApplicationBuilder builder)
    {
        builder.AddIdentityServices();

        builder.Services.AddDatabaseDeveloperPageExceptionFilter();

        builder.Services.AddExceptionHandler<ProblemDetailsExceptionHandler>();

        builder.Services.Configure<ApiBehaviorOptions>(options =>
            options.SuppressModelStateInvalidFilter = true);

        builder.Services.AddEndpointsApiExplorer();

        builder.Services.AddOpenApi(options =>
        {
            options.AddOperationTransformer<ApiExceptionOperationTransformer>();
        });

        builder.Services.AddCors();
        builder.Services.AddAuthorization();
    }
}
