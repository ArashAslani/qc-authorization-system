using qc_authorization.Application.Authorization.Audit.Queries.GetAuditEntries;
using qc_authorization.Application.Common.Interfaces;
using qc_authorization.Domain.Authorization.Audit;
using qc_authorization.Infrastructure.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Shouldly;

namespace qc_authorization.Infrastructure.IntegrationTests.Authorization;

using qc_authorization.Tests.TestSupport;

[TestFixture]
public class AuditQueryIntegrationTests
{
    private ApplicationDbContext _context = null!;
    private IMediator _mediator = null!;
    private ServiceProvider _services = null!;

    [SetUp]
    public async Task SetUp()
    {
        var dbName = $"qc-audit-query-{Guid.NewGuid():N}";
        _services = new ServiceCollection()
            .AddLogging()
            .AddDbContext<ApplicationDbContext>(options => options.UseInMemoryDatabase(dbName))
            .AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<GetAuthorizationAuditEntriesQuery>())
            .AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>())
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
    public async Task Can_Query_Audit_Entries_With_Pagination()
    {
        _context.AuthorizationAuditEntries.AddRange(
            AuthorizationAuditEntry.Create("GrantCreated", TestGuids.CompanyA, "{\"grantId\": 1}"),
            AuthorizationAuditEntry.Create("GrantRevoked", TestGuids.CompanyA, "{\"grantId\": 1}"),
            AuthorizationAuditEntry.Create("RoleAssigned", TestGuids.CompanyB, "{\"roleId\": 2}"));
        await _context.SaveChangesAsync();

        var result = await _mediator.Send(new GetAuthorizationAuditEntriesQuery(PageNumber: 1, PageSize: 10));
        result.TotalCount.ShouldBe(3);
        result.Items.Count.ShouldBe(3);

        var filtered = await _mediator.Send(new GetAuthorizationAuditEntriesQuery(EventType: "RoleAssigned"));
        filtered.TotalCount.ShouldBe(1);
        filtered.Items[0].ActorUserId.ShouldBe(TestGuids.CompanyB);
    }
}
