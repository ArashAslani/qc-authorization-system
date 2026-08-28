using Microsoft.EntityFrameworkCore;
using qc_authorization.Domain.Organization;
using qc_authorization.Domain.Organization.Exceptions;
using qc_authorization.Infrastructure.Data;
using NUnit.Framework;
using Shouldly;

namespace qc_authorization.Infrastructure.IntegrationTests.Organization;

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
        var root = new Position { Code = "ROOT", Name = "Root" };
        _context.Positions.Add(root);
        await _context.SaveChangesAsync();

        var child = new Position { Code = "CHILD", Name = "Child", ParentId = root.Id };
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
        var a = new Position { Code = "A", Name = "A" };
        var b = new Position { Code = "B", Name = "B" };
        var n = new Position { Code = "N", Name = "N" };
        _context.Positions.AddRange(a, b, n);
        await _context.SaveChangesAsync();

        n.ParentId = a.Id;
        await _context.SaveChangesAsync();

        var all = await _context.Positions.AsNoTracking().ToListAsync();
        var aFromDb = all.Single(p => p.Id == a.Id);
        var bFromDb = all.Single(p => p.Id == b.Id);
        var nFromDb = all.Single(p => p.Id == n.Id);

        _hierarchy.EnsureValidParenting(nFromDb, bFromDb, all);

        var tracked = await _context.Positions.SingleAsync(p => p.Id == nFromDb.Id);
        tracked.ParentId = bFromDb.Id;
        await _context.SaveChangesAsync();

        var fresh = await _context.Positions.AsNoTracking().ToListAsync();
        _hierarchy.Ancestors(fresh.Single(p => p.Id == nFromDb.Id), fresh).Single().Id.ShouldBe(bFromDb.Id);
        _hierarchy.Ancestors(fresh.Single(p => p.Id == aFromDb.Id), fresh).ShouldBeEmpty();
    }

    [Test]
    public async Task Re_Parenting_Under_Own_Descendant_Throws()
    {
        var a = new Position { Code = "A", Name = "A" };
        var b = new Position { Code = "B", Name = "B" };
        var d = new Position { Code = "D", Name = "D" };
        _context.Positions.AddRange(a, b, d);
        await _context.SaveChangesAsync();

        b.ParentId = a.Id;
        d.ParentId = b.Id;
        await _context.SaveChangesAsync();

        var all = await _context.Positions.AsNoTracking().ToListAsync();
        var aFromDb = all.Single(p => p.Id == a.Id);
        var dFromDb = all.Single(p => p.Id == d.Id);

        Should.Throw<HierarchyCycleException>(() => _hierarchy.EnsureValidParenting(aFromDb, dFromDb, all));
    }
}
