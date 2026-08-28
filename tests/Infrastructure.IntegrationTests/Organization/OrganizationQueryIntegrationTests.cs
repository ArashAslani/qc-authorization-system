using qc_authorization.Application.Common.Interfaces;
using qc_authorization.Application.Organization.Commands.AssignPersonnelToPosition;
using qc_authorization.Application.Organization.Commands.CreatePersonnel;
using qc_authorization.Application.Organization.Commands.CreatePosition;
using qc_authorization.Application.Organization.Queries.GetPersonnel;
using qc_authorization.Application.Organization.Queries.GetPersonnelById;
using qc_authorization.Application.Organization.Queries.GetPositionAssignments;
using qc_authorization.Application.Organization.Queries.GetPositionById;
using qc_authorization.Application.Organization.Queries.GetPositions;
using qc_authorization.Domain.Organization;
using qc_authorization.Domain.Organization.Enums;
using qc_authorization.Infrastructure.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Shouldly;

namespace qc_authorization.Infrastructure.IntegrationTests.Organization;

[TestFixture]
public class OrganizationQueryIntegrationTests
{
    private ApplicationDbContext _context = null!;
    private IMediator _mediator = null!;
    private ServiceProvider _services = null!;

    [SetUp]
    public async Task SetUp()
    {
        var dbName = $"qc-org-query-{Guid.NewGuid():N}";
        _services = new ServiceCollection()
            .AddLogging()
            .AddDbContext<ApplicationDbContext>(options => options.UseInMemoryDatabase(dbName))
            .AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<GetPersonnelQuery>())
            .AddSingleton<PositionHierarchyService>()
            .AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>())
            .AddIdentityCore<qc_authorization.Infrastructure.Identity.ApplicationUser>()
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .Services
            .AddScoped<IPersonnelIdentityBridge, qc_authorization.Infrastructure.Identity.PersonnelIdentityBridge>()
            .BuildServiceProvider();

        _context = _services.GetRequiredService<ApplicationDbContext>();
        await _context.Database.EnsureCreatedAsync();
        _mediator = _services.GetRequiredService<IMediator>();
    }

    [TearDown]
    public async Task TearDown()
    {
        await _context.Database.EnsureDeletedAsync();
        await _context.DisposeAsync();
        await _services.DisposeAsync();
    }

    [Test]
    public async Task Can_Query_Personnel_List_And_ById()
    {
        var p1Id = await _mediator.Send(new CreatePersonnelCommand("1111111111", "Reza", "Sadeghi", "P-101", Gender: PersonnelGender.Male, Status: PersonnelStatus.Active));
        var p2Id = await _mediator.Send(new CreatePersonnelCommand("2222222222", "Sara", "Karimi", "P-102", Gender: PersonnelGender.Female, Status: PersonnelStatus.Inactive));

        var list = await _mediator.Send(new GetPersonnelQuery());
        list.Count.ShouldBe(2);

        var searchList = await _mediator.Send(new GetPersonnelQuery(SearchTerm: "Karimi"));
        searchList.Count.ShouldBe(1);
        searchList[0].FirstName.ShouldBe("Sara");

        var statusList = await _mediator.Send(new GetPersonnelQuery(Status: PersonnelStatus.Inactive));
        statusList.Count.ShouldBe(1);
        statusList[0].Id.ShouldBe(p2Id);

        var details = await _mediator.Send(new GetPersonnelByIdQuery(p1Id));
        details.Id.ShouldBe(p1Id);
        details.FirstName.ShouldBe("Reza");
    }

    [Test]
    public async Task Can_Query_Positions_And_Hierarchy()
    {
        var rootPosId = await _mediator.Send(new CreatePositionCommand(1, "CEO", "Chief Executive Officer", "Top Level", null));
        var childPosId = await _mediator.Send(new CreatePositionCommand(1, "QC_DIR", "QC Director", "Director Level", rootPosId));

        var allPositions = await _mediator.Send(new GetPositionsQuery());
        allPositions.Count.ShouldBe(2);

        var childrenOfRoot = await _mediator.Send(new GetPositionsQuery(ParentPositionId: rootPosId));
        childrenOfRoot.Count.ShouldBe(1);
        childrenOfRoot[0].Code.ShouldBe("QC_DIR");

        var details = await _mediator.Send(new GetPositionByIdQuery(rootPosId));
        details.Children.Count.ShouldBe(1);
        details.Children[0].Id.ShouldBe(childPosId);
    }

    [Test]
    public async Task Can_Query_PositionAssignments()
    {
        var pId = await _mediator.Send(new CreatePersonnelCommand("3333333333", "Hassan", "Moradi", "P-103"));
        var posId = await _mediator.Send(new CreatePositionCommand(1, "AUDITOR", "Lead Auditor", null, null));

        var from = DateTimeOffset.UtcNow.AddDays(-5);
        await _mediator.Send(new AssignPersonnelToPositionCommand(pId, posId, from));

        var assignments = await _mediator.Send(new GetPositionAssignmentsQuery(PersonnelId: pId));
        assignments.Count.ShouldBe(1);
        assignments[0].PositionCode.ShouldBe("AUDITOR");
        assignments[0].IsActive.ShouldBeTrue();
    }
}
