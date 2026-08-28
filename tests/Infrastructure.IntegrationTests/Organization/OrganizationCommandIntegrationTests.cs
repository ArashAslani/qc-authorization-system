using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using qc_authorization.Application.Authorization.Commands.CreateGrant;
using qc_authorization.Application.Common.Interfaces;
using qc_authorization.Application.Organization.Commands.AssignPersonnelToPosition;
using qc_authorization.Application.Organization.Commands.CreatePersonnel;
using qc_authorization.Application.Organization.Commands.CreatePosition;
using qc_authorization.Domain.Organization;
using qc_authorization.Infrastructure.Data;
using qc_authorization.Infrastructure.IntegrationTests.TestSupport;
using MediatR;
using NUnit.Framework;
using Shouldly;

namespace qc_authorization.Infrastructure.IntegrationTests.Organization;

[TestFixture]
public class OrganizationCommandIntegrationTests
{
    private ApplicationDbContext _context = null!;
    private IMediator _mediator = null!;

    [SetUp]
    public async Task SetUp()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"qc-org-cmd-{Guid.NewGuid():N}")
            .Options;
        _context = new ApplicationDbContext(options);
        await _context.Database.EnsureCreatedAsync();

        _mediator = new ServiceCollection()
            .AddLogging()
            .AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<CreatePositionCommand>())
            .AddSingleton<PositionHierarchyService>()
            .AddScoped<IApplicationDbContext>(_ => _context)
            .BuildServiceProvider()
            .GetRequiredService<IMediator>();
    }

    [TearDown]
    public async Task TearDown()
    {
        await _context.Database.EnsureDeletedAsync();
        await _context.DisposeAsync();
    }

    [Test]
    public async Task Can_Create_Personnel_With_IdentityUserId()
    {
        var id = await _mediator.Send(new CreatePersonnelCommand(
            "1234567890", "Ali", "Ahmadi", "PC-001", IdentityUserId: TestUsers.UserA));

        var p = await _context.Personnel.SingleAsync(x => x.Id == id);
        p.IdentityUserId.ShouldBe(TestUsers.UserA);
    }

    [Test]
    public async Task Can_Assign_Personnel_To_Position()
    {
        var personnelId = await _mediator.Send(new CreatePersonnelCommand(
            "1234567890", "Ali", "Ahmadi", "PC-001", IdentityUserId: TestUsers.UserA));
        var positionId = await _mediator.Send(new CreatePositionCommand(1, "ENG", "Engineer", null, null));
        var from = DateTimeOffset.UtcNow.AddDays(-1);

        var assignmentId = await _mediator.Send(new AssignPersonnelToPositionCommand(
            personnelId, positionId, from));

        var a = await _context.PositionAssignments.SingleAsync(x => x.Id == assignmentId);
        a.PersonnelId.ShouldBe(personnelId);
        a.PositionId.ShouldBe(positionId);
    }
}
