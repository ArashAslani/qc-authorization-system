using AccessManagement.Domain.Organization;
using AccessManagement.Domain.Organization.Enums;
using NUnit.Framework;
using Shouldly;

namespace AccessManagement.Domain.UnitTests.Organization;

using AccessManagement.Tests.TestSupport;

[TestFixture]
public class PersonnelTests
{
    [Test]
    public void Can_Create_Personnel()
    {
        var p = Personnel.Create("1234567890", "Ali", "Ahmadi", "PC-001", "09120000000", PersonnelGender.Male);
        p.NationalId.ShouldBe("1234567890");
        p.FirstName.ShouldBe("Ali");
        p.LastName.ShouldBe("Ahmadi");
        p.PersonnelCode.ShouldBe("PC-001");
        p.Status.ShouldBe(PersonnelStatus.Active);
        p.IsSystemUser.ShouldBeFalse();
        p.FullName.ShouldBe("Ali Ahmadi");
    }

    [Test]
    public void NationalId_And_PersonnelCode_May_Be_Null()
    {
        var p = Personnel.Create(null, "Sys", "Admin", null, isSystemUser: true);
        p.NationalId.ShouldBeNull();
        p.PersonnelCode.ShouldBeNull();
        p.IsSystemUser.ShouldBeTrue();
    }
}
