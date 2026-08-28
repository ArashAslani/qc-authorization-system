using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using qc_authorization.Application.Common.Interfaces;
using qc_authorization.Application.Organization.Commands.CreatePersonnel;
using qc_authorization.Application.Organization.Commands.LinkPersonnelToIdentityUser;
using qc_authorization.Domain.Organization;
using qc_authorization.Infrastructure.Data;
using qc_authorization.Infrastructure.Identity;
using qc_authorization.Infrastructure.IntegrationTests.TestSupport;
using MediatR;
using NUnit.Framework;
using Shouldly;

namespace qc_authorization.Infrastructure.IntegrationTests.Identity;

[TestFixture]
public class PersonnelIdentityBridgeTests
{
    private ServiceProvider _services = null!;
    private IServiceScope _scope = null!;

    [SetUp]
    public void SetUp()
    {
        var dbName = $"qc-personnel-bridge-{Guid.NewGuid():N}";

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

        services.AddScoped<IPersonnelIdentityBridge, PersonnelIdentityBridge>();
        services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssemblyContaining<CreatePersonnelCommand>();
        });

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
    public async Task Link_Syncs_Personnel_And_ApplicationUser()
    {
        var context = _scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var userManager = _scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var bridge = _scope.ServiceProvider.GetRequiredService<IPersonnelIdentityBridge>();

        var user = new ApplicationUser
        {
            Id = TestUsers.UserA,
            UserName = "user.a@test.local",
            Email = "user.a@test.local",
            EmailConfirmed = true,
        };
        (await userManager.CreateAsync(user, "password1")).Succeeded.ShouldBeTrue();

        var personnel = Personnel.Create("123", "Ali", "Ahmadi", "PC-001");
        context.Personnel.Add(personnel);
        await context.SaveChangesAsync();

        await bridge.LinkAsync(personnel.Id, user.Id);

        var linkedPersonnel = await context.Personnel.SingleAsync(p => p.Id == personnel.Id);
        linkedPersonnel.IdentityUserId.ShouldBe(user.Id);

        var linkedUser = await userManager.FindByIdAsync(user.Id.ToString());
        linkedUser!.PersonnelId.ShouldBe(personnel.Id);
    }

    [Test]
    public async Task CreatePersonnel_With_IdentityUserId_Syncs_ApplicationUser_PersonnelId()
    {
        var userManager = _scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var mediator = _scope.ServiceProvider.GetRequiredService<IMediator>();
        var context = _scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var user = new ApplicationUser
        {
            Id = TestUsers.UserA,
            UserName = "user.a@test.local",
            Email = "user.a@test.local",
            EmailConfirmed = true,
        };
        (await userManager.CreateAsync(user, "password1")).Succeeded.ShouldBeTrue();

        var personnelId = await mediator.Send(new CreatePersonnelCommand(
            "1234567890", "Ali", "Ahmadi", "PC-001", IdentityUserId: TestUsers.UserA));

        var personnel = await context.Personnel.SingleAsync(p => p.Id == personnelId);
        personnel.IdentityUserId.ShouldBe(TestUsers.UserA);

        var linkedUser = await userManager.FindByIdAsync(TestUsers.UserA.ToString());
        linkedUser!.PersonnelId.ShouldBe(personnelId);
    }

    [Test]
    public async Task LinkPersonnelCommand_Updates_Both_Sides()
    {
        var userManager = _scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var mediator = _scope.ServiceProvider.GetRequiredService<IMediator>();
        var context = _scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var user = new ApplicationUser
        {
            Id = TestUsers.UserB,
            UserName = "user.b@test.local",
            Email = "user.b@test.local",
            EmailConfirmed = true,
        };
        (await userManager.CreateAsync(user, "password1")).Succeeded.ShouldBeTrue();

        var personnelId = await mediator.Send(new CreatePersonnelCommand(
            "9876543210", "Sara", "Karimi", "PC-002"));

        await mediator.Send(new LinkPersonnelToIdentityUserCommand(personnelId, TestUsers.UserB));

        var personnel = await context.Personnel.SingleAsync(p => p.Id == personnelId);
        personnel.IdentityUserId.ShouldBe(TestUsers.UserB);

        var linkedUser = await userManager.FindByIdAsync(TestUsers.UserB.ToString());
        linkedUser!.PersonnelId.ShouldBe(personnelId);
    }
}
