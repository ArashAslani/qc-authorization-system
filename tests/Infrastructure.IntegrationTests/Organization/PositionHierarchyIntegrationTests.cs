using Microsoft.EntityFrameworkCore;
using qc_authorization.Domain.Organization;
using qc_authorization.Domain.Organization.Exceptions;
using qc_authorization.Infrastructure.Data;
using NUnit.Framework;
using Shouldly;

namespace qc_authorization.Infrastructure.IntegrationTests.Organization;

[TestFixture]
public class PositionHierarchyIntegrationTests
{
    private ApplicationDbContext _context = null!;
    private PositionHierarchyService _hierarchy = null!;

    [SetUp]
    public async Task SetUp()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"qc-it-{Guid.NewGuid()}")
            .Options;

        _context = new ApplicationDbContext(options);
        _hierarchy = new PositionHierarchyService();
        await _context.Database.EnsureCreatedAsync();
    }

    [TearDown]
    public async Task TearDown()
    {
        await _context.Database.EnsureDeletedAsync();
        await _context.DisposeAsync();
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
        _hierarchy.Children(all.Single(p => p.Id == root.Id), all).ShouldHaveSingleItem().Code.ShouldBe("CHILD");
        _hierarchy.Ancestors(all.Single(p => p.Id == child.Id), all).ShouldHaveSingleItem().Id.ShouldBe(root.Id);
    }

    [Test]
    public async Task Can_Re_Parent_Position()
    {
        var a = new Position { Code = "A", Name = "A" };
        var b = new Position { Code = "B", Name = "B" };
        var n = new Position { Code = "N", Name = "N" };
        _context.Positions.AddRange(a, b, n);
        await _context.SaveChangesAsync();

        // n is initially a child of a.
        n.ParentId = a.Id;
        await _context.SaveChangesAsync();

        // Re-parent n under b using a fresh, tracked entity.
        var trackedN = await _context.Positions.FindAsync(n.Id);
        trackedN!.ParentId = b.Id;
        await _context.SaveChangesAsync();

        var fresh = await _context.Positions.AsNoTracking().ToListAsync();
        var ancestors = _hierarchy.Ancestors(fresh.Single(p => p.Id == n.Id), fresh);
        ancestors.ShouldHaveSingleItem().Id.ShouldBe(b.Id);
    }

    [Test]
    public async Task Re_Parenting_Under_Own_Descendant_Throws()
    {
        var a = new Position { Code = "A", Name = "A" };
        var b = new Position { Code = "B", Name = "B", ParentId = 1 };
        var d = new Position { Code = "D", Name = "D", ParentId = 2 };
        _context.Positions.AddRange(a, b, d);
        await _context.SaveChangesAsync();

        var all = await _context.Positions.AsNoTracking().ToListAsync();
        var trackedA = all.Single(p => p.Id == a.Id);
        var trackedD = all.Single(p => p.Id == d.Id);

        Should.Throw<HierarchyCycleException>(() => _hierarchy.EnsureValidParenting(trackedA, trackedD, all));
    }
}
