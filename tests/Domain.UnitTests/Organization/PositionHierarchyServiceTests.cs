using AccessManagement.Domain.Organization;
using AccessManagement.Domain.Organization.Exceptions;
using AccessManagement.Tests.TestSupport;
using NUnit.Framework;
using Shouldly;

namespace AccessManagement.Domain.UnitTests.Organization;

[TestFixture]
public class PositionHierarchyServiceTests
{
    private PositionHierarchyService _service = null!;

    private static readonly Guid Co1 = TestGuids.CompanyA;
    private static readonly Guid Co2 = TestGuids.CompanyB;

    [SetUp]
    public void SetUp() => _service = new PositionHierarchyService();

    [Test]
    public void Create_Position_HasDefaults()
    {
        var position = Position.Create(Co1, "ENG", "Engineer");
        position.Code.ShouldBe("ENG");
        position.Title.ShouldBe("Engineer");
        position.ParentPositionId.ShouldBeNull();
        position.Children.ShouldNotBeNull();
    }

    [Test]
    public void Assign_Parent_SetsParentPositionId()
    {
        var parent = WithId(TestGuids.PosA1, "ROOT", "Root");
        var child = WithId(TestGuids.PosA2, "CHILD", "Child", parent.Id);
        child.ParentPositionId.ShouldBe(parent.Id);
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
        var parent = WithId(Guid.Parse("c0000010-0000-0000-0000-000000000010"), "PARENT", "Parent", companyId: Co2);
        var child = WithId(Guid.Parse("c0000011-0000-0000-0000-000000000011"), "CHILD", "Child", companyId: Co1);
        var all = new List<Position> { parent, child };

        Should.Throw<OrganizationDomainException>(() =>
            _service.EnsureValidParenting(child, parent, all));
    }

    [Test]
    public void Valid_ReParenting_ToAnotherRoot_Is_Accepted()
    {
        var root1 = WithId(Guid.Parse("0a000001-0000-0000-0000-000000000001"), "R1", "R1");
        var root2 = WithId(Guid.Parse("0a000002-0000-0000-0000-000000000002"), "R2", "R2");
        var node = WithId(Guid.Parse("0a000003-0000-0000-0000-000000000003"), "N", "N", root1.Id);

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
        var a = WithId(Guid.Parse("0c000001-0000-0000-0000-000000000001"), "A", "A");
        var b = WithId(Guid.Parse("0c000002-0000-0000-0000-000000000002"), "B", "B", a.Id);
        SetParentPositionId(a, b.Id);
        Should.Throw<HierarchyCycleException>(() => _service.Ancestors(b, new[] { a, b }));
    }

    [Test]
    public void Detecting_Pre_Existing_Cycle_While_Walking_Descendants_Throws()
    {
        var a = WithId(Guid.Parse("0c000003-0000-0000-0000-000000000003"), "A", "A");
        var b = WithId(Guid.Parse("0c000004-0000-0000-0000-000000000004"), "B", "B", a.Id);
        SetParentPositionId(a, b.Id);
        Should.Throw<HierarchyCycleException>(() => _service.Descendants(a, new[] { a, b }));
    }

    private static Position WithId(Guid id, string code, string title, Guid? parentId = null, Guid? companyId = null)
    {
        var position = Position.Create(companyId ?? Co1, code, title, parentPositionId: parentId);
        position.Id = id;
        return position;
    }

    private static void SetParentPositionId(Position position, Guid? parentId) =>
        typeof(Position).GetProperty(nameof(Position.ParentPositionId))!
            .SetValue(position, parentId);

    private static (List<Position> all, Position a, Position b, Position c, Position d) BuildSample()
    {
        var a = WithId(Guid.Parse("01000001-0000-0000-0000-000000000001"), "A", "A");
        var b = WithId(Guid.Parse("01000002-0000-0000-0000-000000000002"), "B", "B", a.Id);
        var c = WithId(Guid.Parse("01000003-0000-0000-0000-000000000003"), "C", "C", a.Id);
        var d = WithId(Guid.Parse("01000004-0000-0000-0000-000000000004"), "D", "D", b.Id);
        return (new List<Position> { a, b, c, d }, a, b, c, d);
    }
}
