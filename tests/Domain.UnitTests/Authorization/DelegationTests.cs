using AccessManagement.Domain.Authorization;
using AccessManagement.Domain.Authorization.Enums;
using AccessManagement.Domain.Authorization.Exceptions;
using AccessManagement.Domain.Authorization.ValueObjects;
using AccessManagement.Tests.TestSupport;
using NUnit.Framework;
using Shouldly;

namespace AccessManagement.Domain.UnitTests.Authorization;

[TestFixture]
public class DelegationTests
{
    private static readonly DateTimeOffset T0 = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
    private static readonly Guid Delegator = Guid.Parse("11111111-1111-1111-1111-111111111101");
    private static readonly Guid Delegate = Guid.Parse("11111111-1111-1111-1111-111111111102");

    [Test]
    public void Can_Create_Delegation()
    {
        var delegation = Delegation.Create(Delegator, Delegate, TestGuids.Permission100, T0, T0.AddDays(7));
        delegation.DelegatorUserId.ShouldBe(Delegator);
        delegation.DelegateUserId.ShouldBe(Delegate);
        delegation.Delegable.ShouldBeFalse();
    }

    [Test]
    public void Cannot_Delegate_To_Self()
    {
        Should.Throw<AuthorizationDomainException>(() =>
            Delegation.Create(Delegator, Delegator, TestGuids.Permission100, T0, null));
    }

    [Test]
    public void Revoked_Delegation_Cannot_Produce_Grant()
    {
        var delegation = Delegation.Create(Delegator, Delegate, TestGuids.Permission100, T0, null);
        delegation.Id = TestGuids.Delegation1;
        delegation.Revoke();

        Should.Throw<AuthorizationDomainException>(() => delegation.ToGrant());
    }

    [Test]
    public void ToGrant_Produces_Delegation_Sourced_Grant()
    {
        var delegation = Delegation.Create(Delegator, Delegate, TestGuids.Permission100, T0, null, TestGuids.CompanyA);
        delegation.Id = TestGuids.Delegation1;

        var grant = delegation.ToGrant();
        grant.SubjectType.ShouldBe(SubjectType.User);
        grant.SubjectId.ShouldBe(Guid.Empty);
        grant.SubjectUserId.ShouldBe(Delegate);
        grant.SourceType.ShouldBe(SourceType.Delegation);
        grant.SourceId.ShouldBe(TestGuids.Delegation1);
        grant.Priority.ShouldBe(SourcePriority.Delegation);
        grant.ScopeUnitId.ShouldBe(TestGuids.CompanyA);
    }
}
