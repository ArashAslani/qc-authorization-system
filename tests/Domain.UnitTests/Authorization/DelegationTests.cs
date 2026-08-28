using qc_authorization.Domain.Authorization;
using qc_authorization.Domain.Authorization.Enums;
using qc_authorization.Domain.Authorization.Exceptions;
using qc_authorization.Domain.Authorization.ValueObjects;
using NUnit.Framework;
using Shouldly;

namespace qc_authorization.Domain.UnitTests.Authorization;

[TestFixture]
public class DelegationTests
{
    private static readonly DateTimeOffset T0 = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
    private static readonly Guid Delegator = Guid.Parse("11111111-1111-1111-1111-111111111101");
    private static readonly Guid Delegate = Guid.Parse("11111111-1111-1111-1111-111111111102");

    [Test]
    public void Can_Create_Delegation()
    {
        var delegation = Delegation.Create(Delegator, Delegate, 100, T0, T0.AddDays(7));
        delegation.DelegatorUserId.ShouldBe(Delegator);
        delegation.DelegateUserId.ShouldBe(Delegate);
        delegation.Delegable.ShouldBeTrue();
    }

    [Test]
    public void Cannot_Delegate_To_Self()
    {
        Should.Throw<AuthorizationDomainException>(() =>
            Delegation.Create(Delegator, Delegator, 100, T0, null));
    }

    [Test]
    public void Revoked_Delegation_Cannot_Produce_Grant()
    {
        var delegation = Delegation.Create(Delegator, Delegate, 100, T0, null);
        delegation.Id = 5001;
        delegation.Revoke();

        Should.Throw<AuthorizationDomainException>(() => delegation.ToGrant());
    }

    [Test]
    public void ToGrant_Produces_Delegation_Sourced_Grant()
    {
        var delegation = Delegation.Create(Delegator, Delegate, 100, T0, null, ScopeKind.Company, "C-1");
        delegation.Id = 5001;

        var grant = delegation.ToGrant();
        grant.SubjectType.ShouldBe(SubjectType.User);
        grant.SubjectId.ShouldBe(0);
        grant.SubjectUserId.ShouldBe(Delegate);
        grant.SourceType.ShouldBe(SourceType.Delegation);
        grant.SourceId.ShouldBe(5001);
        grant.Priority.ShouldBe(SourcePriority.Delegation);
        grant.ScopeKind.ShouldBe(ScopeKind.Company);
        grant.ScopeIdentifier.ShouldBe("C-1");
    }
}
