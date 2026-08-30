using Microsoft.EntityFrameworkCore;
using AccessManagement.Domain.Organization;
using AccessManagement.Domain.Organization.Exceptions;
using AccessManagement.Infrastructure.Data;
using NUnit.Framework;
using Shouldly;

namespace AccessManagement.Infrastructure.IntegrationTests.Organization;

using AccessManagement.Tests.TestSupport;

[TestFixture]
[NonParallelizable]
public class PositionHierarchyIntegrationTests
{
    private ApplicationDbContext _context = null!;
    private PositionHierarchyService _hierarchy = null!;

    [SetUp]
    public async Task SetUp()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"qc-it-{Guid.NewGuid():N}")
            .EnableServiceProviderCaching(false)
            .Options;
        _context = new ApplicationDbContext(options);
        _hierarchy = new PositionHierarchyService();
        await _context.Database.EnsureCreatedAsync();
    }

    [TearDown]
    public async Task TearDown()
    {
        try
        {
            await _context.Database.EnsureDeletedAsync();
        }
        finally
        {
            await _context.DisposeAsync();
        }
    }

    [Test]
    public async Task Can_Create_Position_And_Assign_Parent()
    {
        var root = Position.Create(TestGuids.CompanyA, "ROOT", "Root");
        _context.Positions.Add(root);
        await _context.SaveChangesAsync();

        var child = Position.Create(TestGuids.CompanyA, "CHILD", "Child", parentPositionId: root.Id);
        _context.Positions.Add(child);
        await _context.SaveChangesAsync();

        var all = await _context.Positions.AsNoTracking().ToListAsync();
        var rootFromDb = all.Single(p => p.Id == root.Id);
        var childFromDb = all.Single(p => p.Id == child.Id);

        _hierarchy.Children(rootFromDb, all).ShouldHaveSingleItem().Id.ShouldBe(childFromDb.Id);
        _hierarchy.Ancestors(childFromDb, all).ShouldHaveSingleItem().Id.ShouldBe(rootFromDb.Id);
    }

    [Test]
    public async Task Can_Re_Parent_Position()
    {
        var a = Position.Create(TestGuids.CompanyA, "A", "A");
        var b = Position.Create(TestGuids.CompanyA, "B", "B");
        var n = Position.Create(TestGuids.CompanyA, "N", "N");
        _context.Positions.AddRange(a, b, n);
        await _context.SaveChangesAsync();

        var trackedN = await _context.Positions.SingleAsync(p => p.Id == n.Id);
        var trackedA = await _context.Positions.SingleAsync(p => p.Id == a.Id);
        trackedN.Reparent(trackedA, await _context.Positions.AsNoTracking().ToListAsync(), _hierarchy);
        await _context.SaveChangesAsync();

        var all = await _context.Positions.AsNoTracking().ToListAsync();
        var aFromDb = all.Single(p => p.Id == a.Id);
        var bFromDb = all.Single(p => p.Id == b.Id);
        var nFromDb = all.Single(p => p.Id == n.Id);

        _hierarchy.EnsureValidParenting(nFromDb, bFromDb, all);

        var tracked = await _context.Positions.SingleAsync(p => p.Id == nFromDb.Id);
        tracked.Reparent(bFromDb, all, _hierarchy);
        await _context.SaveChangesAsync();

        var fresh = await _context.Positions.AsNoTracking().ToListAsync();
        _hierarchy.Ancestors(fresh.Single(p => p.Id == nFromDb.Id), fresh).Single().Id.ShouldBe(bFromDb.Id);
        _hierarchy.Ancestors(fresh.Single(p => p.Id == aFromDb.Id), fresh).ShouldBeEmpty();
    }

    [Test]
    public async Task Re_Parenting_Under_Own_Descendant_Throws()
    {
        var a = Position.Create(TestGuids.CompanyA, "A", "A");
        var b = Position.Create(TestGuids.CompanyA, "B", "B");
        var d = Position.Create(TestGuids.CompanyA, "D", "D");
        _context.Positions.AddRange(a, b, d);
        await _context.SaveChangesAsync();

        var trackedB = await _context.Positions.SingleAsync(p => p.Id == b.Id);
        var trackedD = await _context.Positions.SingleAsync(p => p.Id == d.Id);
        var trackedA = await _context.Positions.SingleAsync(p => p.Id == a.Id);

        trackedB.Reparent(trackedA, await _context.Positions.AsNoTracking().ToListAsync(), _hierarchy);
        trackedD.Reparent(trackedB, await _context.Positions.AsNoTracking().ToListAsync(), _hierarchy);
        await _context.SaveChangesAsync();

        var all = await _context.Positions.AsNoTracking().ToListAsync();
        var aFromDb = all.Single(p => p.Id == a.Id);
        var dFromDb = all.Single(p => p.Id == d.Id);

        Should.Throw<HierarchyCycleException>(() => _hierarchy.EnsureValidParenting(aFromDb, dFromDb, all));
    }

    [Test]
    public async Task Cross_Company_Parent_Is_Rejected()
    {
        var companyA = Position.Create(TestGuids.CompanyA, "A", "A");
        var companyBRoot = Position.Create(TestGuids.CompanyB, "B", "B");
        _context.Positions.AddRange(companyA, companyBRoot);
        await _context.SaveChangesAsync();

        var trackedA = await _context.Positions.SingleAsync(p => p.Id == companyA.Id);
        var trackedB = await _context.Positions.SingleAsync(p => p.Id == companyBRoot.Id);
        var all = await _context.Positions.AsNoTracking().ToListAsync();

        Should.Throw<OrganizationDomainException>(() =>
            trackedA.Reparent(trackedB, all, _hierarchy));
    }
}
