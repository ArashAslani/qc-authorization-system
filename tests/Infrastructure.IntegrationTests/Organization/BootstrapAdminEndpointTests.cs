using System.Net;
using System.Net.Http.Json;
using AccessManagement.Infrastructure.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NUnit.Framework;
using Shouldly;

namespace AccessManagement.Infrastructure.IntegrationTests.Organization;

[TestFixture]
public class BootstrapAdminEndpointTests
{
    [Test]
    public async Task Bootstrap_Endpoint_Is_Anonymous_But_Self_Disabling()
    {
        await using var factory = new BootstrapWebApplicationFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
        });

        var identityUserId = Guid.NewGuid();
        var firstBody = new BootstrapAdminBody("001", "Ada", "Admin", "A-1", identityUserId);

        var first = await client.PostAsJsonAsync("/api/organization/bootstrap/admin", firstBody);
        first.StatusCode.ShouldBe(HttpStatusCode.Created);

        var second = await client.PostAsJsonAsync(
            "/api/organization/bootstrap/admin",
            firstBody with { IdentityUserId = Guid.NewGuid() });
        second.StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }

    private sealed record BootstrapAdminBody(
        string NationalId,
        string FirstName,
        string LastName,
        string PersonnelCode,
        Guid IdentityUserId);

    private sealed class BootstrapWebApplicationFactory : WebApplicationFactory<Program>
    {
        private readonly string _databaseName = $"bootstrap-{Guid.NewGuid():N}";
        private readonly InMemoryDatabaseRoot _databaseRoot = new();

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
            builder.UseSetting("Jwt:Key", "qc-authorization-test-signing-key-min-32-chars");
            builder.ConfigureServices(services =>
            {
                RemoveDbContextRegistrations(services);

                services.AddDbContext<ApplicationDbContext>(options =>
                    options.UseInMemoryDatabase(_databaseName, _databaseRoot)
                        .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning)));
            });
        }

        protected override IHost CreateHost(IHostBuilder builder)
        {
            var host = base.CreateHost(builder);
            using var scope = host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            db.Database.EnsureCreated();
            return host;
        }

        private static void RemoveDbContextRegistrations(IServiceCollection services)
        {
            var toRemove = services
                .Where(d =>
                    d.ServiceType == typeof(ApplicationDbContext)
                    || d.ServiceType == typeof(DbContextOptions)
                    || d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>)
                    || (d.ServiceType.IsGenericType
                        && d.ServiceType.GetGenericTypeDefinition() == typeof(IDbContextOptionsConfiguration<>)))
                .ToList();

            foreach (var descriptor in toRemove)
            {
                services.Remove(descriptor);
            }
        }
    }
}
