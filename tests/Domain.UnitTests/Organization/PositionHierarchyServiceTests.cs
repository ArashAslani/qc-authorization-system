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

    [Test]
    public void Create_Position_HasDefaults()
    {
        var position = Position.Create(1, "ENG", "Engineer");
        position.Code.ShouldBe("ENG");
        position.Title.ShouldBe("Engineer");
        position.ParentPositionId.ShouldBeNull();
        position.Children.ShouldNotBeNull();
    }

    [Test]
    public void Assign_Parent_SetsParentPositionId()
    {
        var parent = WithId(1, "ROOT", "Root");
        var child = WithId(2, "CHILD", "Child", parent.Id);
        child.ParentPositionId.ShouldBe(1);
    }

    [Test]
    public void Read_Children_ReturnsDirectChildren()
    {
        var (all, a, b, c, d) = BuildSample();
        _service.Children(a, all).Select(p => p.Id).ShouldBe(new[] { b.Id, c.Id }, ignoreOrder: false);
        _service.Children(b, all).Select(p => p.Id).ShouldBe(new[] { d.Id });
        _service.Children(d, all).ShouldBeEmpty();
    }

    [Test]
    public void Read_Ancestors_ReturnsChainToRoot_ExcludingSelf()
    {
        var (all, a, b, _, d) = BuildSample();
        _service.Ancestors(d, all).Select(p => p.Id).ShouldBe(new[] { b.Id, a.Id });
        _service.Ancestors(a, all).ShouldBeEmpty();
    }

    [Test]
    public void Read_Descendants_ReturnsSubtree_ExcludingSelf()
    {
        var (all, a, b, c, d) = BuildSample();
        _service.Descendants(a, all).Select(p => p.Id).ShouldBe(new[] { b.Id, c.Id, d.Id });
        _service.Descendants(b, all).Select(p => p.Id).ShouldBe(new[] { d.Id });
        _service.Descendants(d, all).ShouldBeEmpty();
    }

    [Test]
    public void Valid_Hierarchy_Is_Accepted()
    {
        var (all, a, b, _, _) = BuildSample();
        Should.NotThrow(() => _service.EnsureValidParenting(b, a, all));
    }

    [Test]
    public void Self_Parent_Is_Rejected()
    {
        var (all, a, _, _, _) = BuildSample();
        Should.Throw<HierarchyCycleException>(() =>
            _service.EnsureValidParenting(a, a, all));
    }

    [Test]
    public void Indirect_Cycle_Is_Rejected()
    {
        var (all, _, b, _, d) = BuildSample();
        Should.Throw<HierarchyCycleException>(() =>
            _service.EnsureValidParenting(b, d, all));
    }

    [Test]
    public void Cross_Company_Parent_Is_Rejected()
    {
        var parent = WithId(10, "PARENT", "Parent", companyId: 2);
        var child = WithId(11, "CHILD", "Child", companyId: 1);
        var all = new List<Position> { parent, child };

        Should.Throw<OrganizationDomainException>(() =>
            _service.EnsureValidParenting(child, parent, all));
    }

    [Test]
    public void Valid_ReParenting_ToAnotherRoot_Is_Accepted()
    {
        var root1 = WithId(1, "R1", "R1");
        var root2 = WithId(2, "R2", "R2");
        var node = WithId(3, "N", "N", root1.Id);

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
        var a = WithId(1, "A", "A");
        var b = WithId(2, "B", "B", 1);
        SetParentPositionId(a, 2);
        Should.Throw<HierarchyCycleException>(() => _service.Ancestors(b, new[] { a, b }));
    }

    [Test]
    public void Detecting_Pre_Existing_Cycle_While_Walking_Descendants_Throws()
    {
        var a = WithId(1, "A", "A");
        var b = WithId(2, "B", "B", 1);
        SetParentPositionId(a, 2);
        Should.Throw<HierarchyCycleException>(() => _service.Descendants(a, new[] { a, b }));
    }

    private static Position WithId(int id, string code, string title, int? parentId = null, int companyId = 1)
    {
        var position = Position.Create(companyId, code, title, parentPositionId: parentId);
        position.Id = id;
        return position;
    }

    private static void SetParentPositionId(Position position, int? parentId) =>
        typeof(Position).GetProperty(nameof(Position.ParentPositionId))!
            .SetValue(position, parentId);

    private static (List<Position> all, Position a, Position b, Position c, Position d) BuildSample()
    {
        var a = WithId(1, "A", "A");
        var b = WithId(2, "B", "B", 1);
        var c = WithId(3, "C", "C", 1);
        var d = WithId(4, "D", "D", 2);
        return (new List<Position> { a, b, c, d }, a, b, c, d);
    }
}
