using qc_authorization.Domain.Authorization;
using NUnit.Framework;
using Shouldly;

namespace qc_authorization.Domain.UnitTests.Authorization;

[TestFixture]
public class RoleTests
{
    [Test]
    public void Can_Create_Role()
    {
        var r = Role.Create("HR_MANAGER", "HR Manager");
        r.Code.ShouldBe("HR_MANAGER");
        r.Name.ShouldBe("HR Manager");
    }

    [Test]
    public void Can_Assign_Permission_To_Role()
    {
        var r = Role.Create("HR_MANAGER", "HR Manager");
        var p = Permission.Create("PERSONNEL.READ", "Personnel", "Read");

        r.Permissions.Add(new RolePermission { Role = r, Permission = p });

        r.Permissions.Count.ShouldBe(1);
        r.Permissions.Single().Permission.Code.ShouldBe("PERSONNEL.READ");
    }
}
