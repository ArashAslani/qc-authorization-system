using Microsoft.Extensions.DependencyInjection;
using qc_authorization.Application.Authorization.Commands.CreateDelegation;
using qc_authorization.Application.Authorization.Delegation;
using qc_authorization.Application.Authorization.Evaluation;
using qc_authorization.Application.Common.Interfaces;
using qc_authorization.Application.UnitTests.TestSupport;
using qc_authorization.Domain.Authorization;
using qc_authorization.Domain.Authorization.Enums;
using qc_authorization.Domain.Authorization.Exceptions;
using qc_authorization.Domain.Authorization.ValueObjects;
using qc_authorization.Domain.Organization;
using qc_authorization.Domain.Organization.Enums;
using qc_authorization.Infrastructure.Data;
using MediatR;
using NUnit.Framework;
using Shouldly;

namespace qc_authorization.Application.UnitTests.Authorization;

using qc_authorization.Tests.TestSupport;

[TestFixture]
public class DelegationHierarchyTests
{
    private ApplicationDbContext _context = null!;
    private IMediator _mediator = null!;
    private Permission _perm = null!;
    private static readonly DateTimeOffset T0 = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    [SetUp]
    public async Task SetUp()
    {
        (_context, _) = AuthorizationTestContext.Create();
        await _context.Database.EnsureCreatedAsync();

        _perm = Permission.Create("PERSONNEL.READ", "Personnel", "Read");
        _context.Permissions.Add(_perm);
        await _context.SaveChangesAsync();

        _mediator = AuthorizationTestContext.CreateMediatorServices(_context).GetRequiredService<IMediator>();
    }

    [TearDown]
    public async Task TearDown()
    {
        await _context.Database.EnsureDeletedAsync();
        await _context.DisposeAsync();
    }

    [Test]
    public async Task Delegation_Allowed_When_Delegatee_Is_Subordinate()
    {
        await SeedHierarchyWithScopedGrantAsync();

        await Should.NotThrowAsync(async () =>
            await _mediator.Send(new CreateDelegationCommand(
                TestUsers.UserA, TestUsers.UserB, _perm.Id, T0, null,
                ScopeKind.Company, TestGuids.CompanyA.ToString())));
    }

    [Test]
    public async Task Delegation_Rejected_When_Delegatee_Is_Not_Subordinate_And_No_Unbounded_Access()
    {
        await SeedHierarchyWithScopedGrantAsync();

        await Should.ThrowAsync<AuthorizationDomainException>(async () =>
            await _mediator.Send(new CreateDelegationCommand(
                TestUsers.UserA, TestUsers.UserC, _perm.Id, T0, null,
                ScopeKind.Company, TestGuids.CompanyA.ToString())));
    }

    [Test]
    public async Task Delegation_Allowed_With_Unbounded_Delegator_Grant_Without_Hierarchy()
    {
        _context.Grants.Add(Grant.CreateForUser(
            TestUsers.UserA, _perm.Id, SourceType.User, Guid.Empty, Effect.Allow, T0.AddDays(-30), null,
            SourcePriority.IndividualOverride));
        await _context.SaveChangesAsync();

        await Should.NotThrowAsync(async () =>
            await _mediator.Send(new CreateDelegationCommand(
                TestUsers.UserA, TestUsers.UserC, _perm.Id, T0, null,
                ScopeKind.Company, TestGuids.CompanyA.ToString())));
    }

    private async Task SeedHierarchyWithScopedGrantAsync()
    {
        var managerPosition = Position.Create(TestGuids.CompanyA, "MGR", "Manager");
        managerPosition.Id = TestGuids.PosA1;
        var staffPosition = Position.Create(TestGuids.CompanyA, "STAFF", "Staff", parentPositionId: managerPosition.Id);
        staffPosition.Id = TestGuids.PosA2;
        var peerPosition = Position.Create(TestGuids.CompanyA, "PEER", "Peer");
        peerPosition.Id = Guid.NewGuid();

        var managerPersonnel = Personnel.Create("1111111111", "Manager", "One", "P001");
        managerPersonnel.Id = TestGuids.Personnel1;
        managerPersonnel.LinkIdentityUser(TestUsers.UserA);

        var staffPersonnel = Personnel.Create("2222222222", "Staff", "Two", "P002");
        staffPersonnel.Id = Guid.Parse("c2222222-2222-2222-2222-222222222222");
        staffPersonnel.LinkIdentityUser(TestUsers.UserB);

        var peerPersonnel = Personnel.Create("3333333333", "Peer", "Three", "P003");
        peerPersonnel.Id = Guid.Parse("c3333333-3333-3333-3333-333333333333");
        peerPersonnel.LinkIdentityUser(TestUsers.UserC);

        _context.Positions.AddRange(managerPosition, staffPosition, peerPosition);
        _context.Personnel.AddRange(managerPersonnel, staffPersonnel, peerPersonnel);
        _context.PositionAssignments.AddRange(
            PositionAssignment.Create(managerPersonnel.Id, managerPosition.Id, T0.AddDays(-30)),
            PositionAssignment.Create(staffPersonnel.Id, staffPosition.Id, T0.AddDays(-30)),
            PositionAssignment.Create(peerPersonnel.Id, peerPosition.Id, T0.AddDays(-30)));

        _context.Grants.Add(Grant.CreateForUser(
            TestUsers.UserA,
            _perm.Id,
            SourceType.User,
            Guid.Empty,
            Effect.Allow,
            T0.AddDays(-30),
            null,
            SourcePriority.IndividualOverride,
            scopeKind: ScopeKind.Company,
            scopeIdentifier: TestGuids.CompanyA.ToString()));

        await _context.SaveChangesAsync();
    }
}
