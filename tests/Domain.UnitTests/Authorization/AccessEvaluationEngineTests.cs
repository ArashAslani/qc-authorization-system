using AccessManagement.Domain.Authorization;
using AccessManagement.Domain.Authorization.Enums;
using AccessManagement.Domain.Authorization.ValueObjects;
using AccessManagement.Domain.Organization;
using AccessManagement.Domain.Organization.Exceptions;
using AccessManagement.Tests.TestSupport;
using NUnit.Framework;
using Shouldly;

namespace AccessManagement.Domain.UnitTests.Authorization;

[TestFixture]
public class GrantPriorityTests
{
    private static readonly DateTimeOffset T0 = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    [Test]
    public void Individual_Override_Outranks_Role()
    {
        SourcePriority.IndividualOverride.ShouldBeGreaterThan(SourcePriority.RoleOrRoleGroup);
        SourcePriority.PositionOverride.ShouldBeGreaterThan(SourcePriority.Delegation);
        SourcePriority.Delegation.ShouldBeGreaterThan(SourcePriority.RoleOrRoleGroup);
    }

    [Test]
    public void Grant_Stores_ScopeUnitId_As_Dumb_Data()
    {
        var grant = Grant.Create(
            SubjectType.Position,
            TestGuids.PosA1,
            TestGuids.Permission1,
            SourceType.Position,
            TestGuids.PosA1,
            Effect.Allow,
            T0,
            null,
            SourcePriority.PositionOverride,
            scopeUnitId: TestGuids.CompanyA);

        grant.ScopeUnitId.ShouldBe(TestGuids.CompanyA);
        grant.Effect.ShouldBe(Effect.Allow);
    }

    [Test]
    public void Company_Positions_Cannot_Cross_Parent()
    {
        var companyA = OrganizationalUnit.Create(OrganizationalUnitTypes.Company, "A");
        companyA.Id = TestGuids.CompanyA;
        var companyB = OrganizationalUnit.Create(OrganizationalUnitTypes.Company, "B");
        companyB.Id = TestGuids.CompanyB;

        var manager = Position.Create(companyA.Id, "MGR", "Manager");
        var other = Position.Create(companyB.Id, "MGR", "Manager B");
        var hierarchy = new PositionHierarchyService();

        Should.Throw<OrganizationDomainException>(() =>
            manager.Reparent(other, [manager, other], hierarchy));
    }
}
