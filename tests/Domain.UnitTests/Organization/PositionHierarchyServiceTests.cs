using qc_authorization.Domain.Organization;
using qc_authorization.Domain.Organization.Exceptions;
using NUnit.Framework;
using Shouldly;

namespace qc_authorization.Domain.UnitTests.Organization;

[TestFixture]
public class PositionHierarchyServiceTests
{
    private PositionHierarchyService _service = null!;

    [SetUp]
    public void SetUp() => _service = new PositionHierarchyService();

    // --- create position ---
    [Test]
    public void Create_Position_HasIdAndDefaults()
    {
        var position = new Position { Code = "ENG", Name = "Engineer" };
        position.Code.ShouldBe("ENG");
        position.Name.ShouldBe("Engineer");
        position.ParentId.ShouldBeNull();
        position.Children.ShouldNotBeNull();
    }

    // --- assign parent ---
    [Test]
    public void Assign_Parent_SetsParentId()
    {
        var parent = new Position { Id = 1, Code = "ROOT", Name = "Root" };
        var child = new Position { Id = 2, Code = "CHILD", Name = "Child", ParentId = parent.Id };
        child.ParentId.ShouldBe(1);
    }

    // --- read children ---
    [Test]
    public void Read_Children_ReturnsDirectChildren()
    {
        var (all, a, b, c, d) = BuildSample();
        _service.Children(a, all).Select(p => p.Id).ShouldBe(new[] { b.Id, c.Id }, ignoreOrder: false);
        _service.Children(b, all).Select(p => p.Id).ShouldBe(new[] { d.Id });
        _service.Children(d, all).ShouldBeEmpty();
    }

    // --- read ancestors ---
    [Test]
    public void Read_Ancestors_ReturnsChainToRoot_ExcludingSelf()
    {
        var (all, a, b, _, d) = BuildSample();
        _service.Ancestors(d, all).Select(p => p.Id).ShouldBe(new[] { b.Id, a.Id });
    }

    [Test]
    public void Read_Ancestors_OfRoot_IsEmpty()
    {
        var (all, a, _, _, _) = BuildSample();
        _service.Ancestors(a, all).ShouldBeEmpty();
    }

    // --- read descendants ---
    [Test]
    public void Read_Descendants_ReturnsAllBelow_ExcludingSelf()
    {
        var (all, a, b, c, d) = BuildSample();
        _service.Descendants(a, all).Select(p => p.Id).ShouldBe(new[] { b.Id, c.Id, d.Id });
    }

    [Test]
    public void Read_Descendants_OfLeaf_IsEmpty()
    {
        var (all, _, _, _, d) = BuildSample();
        _service.Descendants(d, all).ShouldBeEmpty();
    }

    // --- valid hierarchy ---
    [Test]
    public void Valid_Hierarchy_Accepts_ValidParenting()
    {
        var (all, a, b, _, _) = BuildSample();
        Should.NotThrow(() => _service.EnsureValidParenting(b, a, all));
    }

    // --- self-parent ---
    [Test]
    public void Self_Parent_Is_Rejected()
    {
        var a = new Position { Id = 1, Code = "A", Name = "A" };
        Should.Throw<HierarchyCycleException>(() =>
            _service.EnsureValidParenting(a, a, new[] { a }));
    }

    // --- indirect cycle ---
    [Test]
    public void Indirect_Cycle_Is_Rejected_OnParenting()
    {
        // Existing data already has a cycle (bad input). Parenting should
        // detect it.
        var a = new Position { Id = 1, Code = "A", Name = "A" };
        var b = new Position { Id = 2, Code = "B", Name = "B", ParentId = 1 };
        a.ParentId = 2; // creates A -> B -> A

        Should.Throw<HierarchyCycleException>(() =>
            _service.EnsureValidParenting(b, a, new[] { a, b }));
    }

    [Test]
    public void Indirect_Cycle_Is_Rejected_WhenReParentingUnderOwnDescendant()
    {
        // Tree: a -> b -> d
        // Trying to re-parent a under d should fail because d's ancestor
        // chain is [b], and re-parenting a under d would put b under a's
        // path (cycle).
        var (all, a, b, _, d) = BuildSample();
        Should.Throw<HierarchyCycleException>(() =>
            _service.EnsureValidParenting(a, d, all));
    }

    // --- invalid re-parenting ---
    [Test]
    public void Invalid_ReParenting_IntoSelf_Is_Rejected()
    {
        var (all, a, _, _, _) = BuildSample();
        Should.Throw<HierarchyCycleException>(() =>
            _service.EnsureValidParenting(a, a, all));
    }

    [Test]
    public void Invalid_ReParenting_IntoDescendant_Is_Rejected()
    {
        // Tree: a -> b -> d. Re-parent b under d would cycle.
        var (all, _, b, _, d) = BuildSample();
        Should.Throw<HierarchyCycleException>(() =>
            _service.EnsureValidParenting(b, d, all));
    }

    // --- valid re-parenting ---
    [Test]
    public void Valid_ReParenting_ToAnotherRoot_Is_Accepted()
    {
        var root1 = new Position { Id = 1, Code = "R1", Name = "R1" };
        var root2 = new Position { Id = 2, Code = "R2", Name = "R2" };
        var node = new Position { Id = 3, Code = "N", Name = "N", ParentId = root1.Id };

        var all = new List<Position> { root1, root2, node };
        Should.NotThrow(() => _service.EnsureValidParenting(node, root2, all));
    }

    [Test]
    public void Detaching_A_Node_By_Setting_NullParent_Is_Accepted()
    {
        var (all, a, b, _, _) = BuildSample();
        Should.NotThrow(() => _service.EnsureValidParenting(b, null, all));
    }

    [Test]
    public void Detecting_Pre_Existing_Cycle_While_Walking_Ancestors_Throws()
    {
        var a = new Position { Id = 1, Code = "A", Name = "A" };
        var b = new Position { Id = 2, Code = "B", Name = "B", ParentId = 1 };
        a.ParentId = 2; // A -> B -> A
        Should.Throw<HierarchyCycleException>(() => _service.Ancestors(b, new[] { a, b }));
    }

    [Test]
    public void Detecting_Pre_Existing_Cycle_While_Walking_Descendants_Throws()
    {
        var a = new Position { Id = 1, Code = "A", Name = "A" };
        var b = new Position { Id = 2, Code = "B", Name = "B", ParentId = 1 };
        a.ParentId = 2;
        Should.Throw<HierarchyCycleException>(() => _service.Descendants(a, new[] { a, b }));
    }

    // a
    // ├── b
    // │   └── d
    // └── c
    private static (List<Position> all, Position a, Position b, Position c, Position d) BuildSample()
    {
        var a = new Position { Id = 1, Code = "A", Name = "A" };
        var b = new Position { Id = 2, Code = "B", Name = "B", ParentId = 1 };
        var c = new Position { Id = 3, Code = "C", Name = "C", ParentId = 1 };
        var d = new Position { Id = 4, Code = "D", Name = "D", ParentId = 2 };
        return (new List<Position> { a, b, c, d }, a, b, c, d);
    }
}
