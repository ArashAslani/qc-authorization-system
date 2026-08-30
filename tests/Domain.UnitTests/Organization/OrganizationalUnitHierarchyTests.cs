using AccessManagement.Domain.Organization;
using AccessManagement.Domain.Organization.Exceptions;
using NUnit.Framework;
using Shouldly;

namespace AccessManagement.Domain.UnitTests.Organization;

[TestFixture]
public class OrganizationalUnitHierarchyTests
{
    [Test]
    public void Ancestors_Walks_To_Root()
    {
        var holding = OrganizationalUnit.Create(OrganizationalUnitTypes.Holding, "H");
        var company = OrganizationalUnit.Create(OrganizationalUnitTypes.Company, "C", holding.Id);
        var station = OrganizationalUnit.Create("Workstation", "W", company.Id);
        var all = new[] { holding, company, station };

        OrganizationalUnitHierarchy.Ancestors(station, all).Select(u => u.Id)
            .ShouldBe(new[] { company.Id, holding.Id });
    }

    [Test]
    public void Cycle_On_Reparent_Is_Rejected()
    {
        var a = OrganizationalUnit.Create(OrganizationalUnitTypes.Company, "A");
        var b = OrganizationalUnit.Create("Workstation", "B", a.Id);
        var all = new[] { a, b };

        Should.Throw<HierarchyCycleException>(() => a.Reparent(b, all));
    }

    [Test]
    public void Self_Parent_Is_Rejected()
    {
        var a = OrganizationalUnit.Create(OrganizationalUnitTypes.Company, "A");
        Should.Throw<HierarchyCycleException>(() =>
            OrganizationalUnitHierarchy.EnsureValidParenting(a, a, new[] { a }));
    }

    [Test]
    public void IsDescendantOf_Includes_Self()
    {
        var company = OrganizationalUnit.Create(OrganizationalUnitTypes.Company, "C");
        OrganizationalUnitHierarchy.IsDescendantOf(company.Id, company.Id, new[] { company }).ShouldBeTrue();
    }
}
