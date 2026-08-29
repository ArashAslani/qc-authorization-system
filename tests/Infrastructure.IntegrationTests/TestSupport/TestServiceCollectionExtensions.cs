using Microsoft.Extensions.DependencyInjection;
using qc_authorization.Application.Common.Interfaces;
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
}
