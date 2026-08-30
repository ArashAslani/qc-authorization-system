using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using AccessManagement.Infrastructure.Data;
using AccessManagement.Infrastructure.Identity;
using AccessManagement.Infrastructure.IntegrationTests.TestSupport;
using NUnit.Framework;
using Shouldly;

namespace AccessManagement.Infrastructure.IntegrationTests.Identity;

using AccessManagement.Tests.TestSupport;

[TestFixture]
public class IdentityIntegrationTests
{
    private ServiceProvider _services = null!;
    private IServiceScope _scope = null!;

    [SetUp]
    public void SetUp()
    {
        var dbName = $"qc-identity-{Guid.NewGuid():N}";

        var services = new ServiceCollection()
            .AddLogging()
            .AddSingleton<IHttpContextAccessor, HttpContextAccessor>()
            .AddDbContext<ApplicationDbContext>(options => options.UseInMemoryDatabase(dbName));

        services
            .AddIdentityCore<ApplicationUser>(o =>
            {
                o.Password.RequireDigit = true;
                o.Password.RequireLowercase = true;
                o.Password.RequireUppercase = false;
                o.Password.RequireNonAlphanumeric = false;
                o.Password.RequiredLength = 8;
                o.User.RequireUniqueEmail = true;
            })
            .AddRoles<ApplicationRole>()
            .AddEntityFrameworkStores<ApplicationDbContext>();

        _services = services.BuildServiceProvider();
        _scope = _services.CreateScope();
        _scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().Database.EnsureCreated();
    }

    [TearDown]
    public async Task TearDown()
    {
        var context = _scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await context.Database.EnsureDeletedAsync();
        _scope.Dispose();
        await _services.DisposeAsync();
    }

    [Test]
    public async Task Can_Create_User()
    {
        var userManager = _scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = new ApplicationUser
        {
            Id = TestUsers.UserA,
            UserName = "user.a@test.local",
            Email = "user.a@test.local",
            EmailConfirmed = true,
        };

        var result = await userManager.CreateAsync(user, "password1");
        result.Succeeded.ShouldBeTrue();

        var stored = await userManager.FindByIdAsync(TestUsers.UserA.ToString());
        stored.ShouldNotBeNull();
        stored!.Email.ShouldBe("user.a@test.local");
    }

    [Test]
    public async Task Can_Sign_In_With_Valid_Credentials()
    {
        var userManager = _scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var user = new ApplicationUser
        {
            Id = TestUsers.UserA,
            UserName = "user.a@test.local",
            Email = "user.a@test.local",
            EmailConfirmed = true,
        };
        (await userManager.CreateAsync(user, "password1")).Succeeded.ShouldBeTrue();

        var stored = await userManager.FindByEmailAsync("user.a@test.local");
        stored.ShouldNotBeNull();

        (await userManager.CheckPasswordAsync(stored!, "password1")).ShouldBeTrue();
        (await userManager.CheckPasswordAsync(stored!, "wrong-pass")).ShouldBeFalse();
    }
}
