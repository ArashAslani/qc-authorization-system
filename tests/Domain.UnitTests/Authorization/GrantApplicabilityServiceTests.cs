using qc_authorization.Domain.Authorization;
using qc_authorization.Domain.Authorization.Enums;
using qc_authorization.Domain.Authorization.Services;
using qc_authorization.Domain.Authorization.ValueObjects;
using qc_authorization.Domain.Organization;
using qc_authorization.Tests.TestSupport;
using NUnit.Framework;
using Shouldly;

namespace qc_authorization.Domain.UnitTests.Authorization;

[TestFixture]
public class GrantApplicabilityServiceTests
{
    private GrantApplicabilityService _service = null!;

    [SetUp]
    public void SetUp() => _service = new GrantApplicabilityService(new PositionHierarchyService());

    [Test]
    public void User_Grant_From_RoleGroup_Applies_To_User_Request()
    {
        var userId = Guid.NewGuid();
        var roleGroupId = Guid.NewGuid();
        var permission = Permission.Create("REPORT.VIEW", "REPORT", "VIEW");

        var grant = Grant.CreateForUser(
            userId,
            permission.Id,
            SourceType.RoleGroup,
            roleGroupId,
            Effect.Allow,
            DateTimeOffset.UtcNow.AddDays(-1),
            null,
            SourcePriority.RoleOrRoleGroup);

        var applies = _service.Applies(
            grant,
            SubjectType.User,
            Guid.Empty,
            userId,
            new HashSet<Guid>(),
            Array.Empty<Position>());

        applies.ShouldBeTrue();
    }

    [Test]
    public void User_Grant_From_RoleGroup_Does_Not_Apply_To_Other_User()
    {
        var userId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var permission = Permission.Create("REPORT.VIEW", "REPORT", "VIEW");

        var grant = Grant.CreateForUser(
            userId,
            permission.Id,
            SourceType.RoleGroup,
            Guid.NewGuid(),
            Effect.Allow,
            DateTimeOffset.UtcNow.AddDays(-1),
            null,
            SourcePriority.RoleOrRoleGroup);

        var applies = _service.Applies(
            grant,
            SubjectType.User,
            Guid.Empty,
            otherUserId,
            new HashSet<Guid>(),
            Array.Empty<Position>());

        applies.ShouldBeFalse();
    }
}
