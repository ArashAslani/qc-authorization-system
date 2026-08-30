using AccessManagement.Domain.Authorization;
using NUnit.Framework;
using Shouldly;

namespace AccessManagement.Domain.UnitTests.Authorization;

using AccessManagement.Tests.TestSupport;

[TestFixture]
public class PermissionTests
{
    [Test]
    public void Can_Create_Permission()
    {
        var p = Permission.Create("PERSONNEL.UPDATE", "Personnel", "Update");
        p.Code.ShouldBe("PERSONNEL.UPDATE");
        p.Resource.ShouldBe("PERSONNEL");
        p.Action.ShouldBe("UPDATE");
    }
}
