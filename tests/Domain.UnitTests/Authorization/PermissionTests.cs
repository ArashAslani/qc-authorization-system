using qc_authorization.Domain.Authorization;
using NUnit.Framework;
using Shouldly;

namespace qc_authorization.Domain.UnitTests.Authorization;

[TestFixture]
public class PermissionTests
{
    [Test]
    public void Can_Create_Permission()
    {
        var p = Permission.Create("PERSONNEL.UPDATE", "Personnel", "Update");
        p.Code.ShouldBe("PERSONNEL.UPDATE");
        p.Resource.ShouldBe("Personnel");
        p.Action.ShouldBe("Update");
    }
}
