using qc_authorization.Domain.Organization;
using qc_authorization.Domain.Organization.Enums;
using NUnit.Framework;
using Shouldly;

namespace qc_authorization.Domain.UnitTests.Organization;

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
        p.PersonalCode.ShouldBe("PC-001");
        p.Status.ShouldBe(PersonnelStatus.Active);
    }
}
