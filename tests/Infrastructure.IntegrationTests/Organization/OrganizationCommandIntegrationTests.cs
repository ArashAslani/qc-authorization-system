using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using AccessManagement.Application.Common.Interfaces;
using AccessManagement.Application.Organization.Commands.AssignPersonnelToPosition;
using AccessManagement.Application.Organization.Commands.CreatePersonnel;
using AccessManagement.Application.Organization.Commands.CreatePosition;
using AccessManagement.Domain.Organization;
using AccessManagement.Infrastructure.Data;
using AccessManagement.Infrastructure.Identity;
using AccessManagement.Infrastructure.IntegrationTests.TestSupport;
using MediatR;
using NUnit.Framework;
using Shouldly;

namespace AccessManagement.Infrastructure.IntegrationTests.Organization;

using AccessManagement.Tests.TestSupport;

[TestFixture]
public class OrganizationCommandIntegrationTests
{
    private ServiceProvider _services = null!;
    private ApplicationDbContext _context = null!;
    private IMediator _mediator = null!;
    private UserManager<ApplicationUser> _userManager = null!;

    [SetUp]
    public async Task SetUp()
    {
        var dbName = $"qc-org-cmd-{Guid.NewGuid():N}";

        _services = new ServiceCollection()
            .AddLogging()
            .AddSingleton<IHttpContextAccessor, HttpContextAccessor>()
            .AddDbContext<ApplicationDbContext>(options => options.UseInMemoryDatabase(dbName))
            .AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<CreatePositionCommand>())
            .AddSingleton<PositionHierarchyService>()
            .AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>())
            .AddIdentityCore<ApplicationUser>(o =>
            {
                o.Password.RequireDigit = true;
                o.Password.RequireLowercase = true;
                o.Password.RequireUppercase = false;
                o.Password.RequireNonAlphanumeric = false;
                o.Password.RequiredLength = 8;
                o.User.RequireUniqueEmail = true;
            })
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .Services
            .AddScoped<IPersonnelIdentityBridge, PersonnelIdentityBridge>()
            .BuildServiceProvider();

        _context = _services.GetRequiredService<ApplicationDbContext>();
        await _context.Database.EnsureCreatedAsync();
        _mediator = _services.GetRequiredService<IMediator>();
        _userManager = _services.GetRequiredService<UserManager<ApplicationUser>>();
    }

    [TearDown]
    public async Task TearDown()
    {
        await _context.Database.EnsureDeletedAsync();
        await _context.DisposeAsync();
        _userManager.Dispose();
        await _services.DisposeAsync();
    }

    [Test]
    public async Task Can_Create_Personnel_With_IdentityUserId()
    {
        (await _userManager.CreateAsync(new ApplicationUser
        {
            Id = TestUsers.UserA,
            UserName = "user.a@test.local",
            Email = "user.a@test.local",
            EmailConfirmed = true,
        }, "password1")).Succeeded.ShouldBeTrue();

        var id = await _mediator.Send(new CreatePersonnelCommand(
            "1234567890", "Ali", "Ahmadi", "PC-001", IdentityUserId: TestUsers.UserA));

        var p = await _context.Personnel.SingleAsync(x => x.Id == id);
        p.IdentityUserId.ShouldBe(TestUsers.UserA);

        var linkedUser = await _context.Users.SingleAsync(u => u.Id == TestUsers.UserA);
        linkedUser.PersonnelId.ShouldBe(id);
    }

    [Test]
    public async Task Can_Assign_Personnel_To_Position()
    {
        var personnelId = await _mediator.Send(new CreatePersonnelCommand(
            "1234567890", "Ali", "Ahmadi", "PC-001"));
        var positionId = await _mediator.Send(new CreatePositionCommand(TestGuids.CompanyA, "ENG", "Engineer", null, null));
        var from = DateTimeOffset.UtcNow.AddDays(-1);

        var assignmentId = await _mediator.Send(new AssignPersonnelToPositionCommand(
            personnelId, positionId, from));

        var a = await _context.PositionAssignments.SingleAsync(x => x.Id == assignmentId);
        a.PersonnelId.ShouldBe(personnelId);
        a.PositionId.ShouldBe(positionId);
    }
}
