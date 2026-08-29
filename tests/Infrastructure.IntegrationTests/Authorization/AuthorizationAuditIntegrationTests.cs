using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using qc_authorization.Application.Authorization.Audit;
using qc_authorization.Application.Authorization.Commands.CreateDelegation;
using qc_authorization.Application.Authorization.Commands.CreateGrant;
using qc_authorization.Application.Authorization.Commands.RevokeDelegation;
using qc_authorization.Application.Authorization.Commands.RevokeGrant;
using qc_authorization.Application.Authorization.Delegation;
using qc_authorization.Application.Authorization.Evaluation;
using qc_authorization.Application.Common.Interfaces;
using qc_authorization.Domain.Authorization;
using qc_authorization.Domain.Authorization.Enums;
using qc_authorization.Domain.Authorization.Evaluation;
using qc_authorization.Domain.Authorization.Services;
using qc_authorization.Domain.Authorization.ValueObjects;
using qc_authorization.Domain.Organization;
using qc_authorization.Infrastructure.Data;
using qc_authorization.Infrastructure.IntegrationTests.TestSupport;
using MediatR;
using NUnit.Framework;
using Shouldly;

namespace qc_authorization.Infrastructure.IntegrationTests.Authorization;

using qc_authorization.Tests.TestSupport;

[TestFixture]
public class AuthorizationAuditIntegrationTests
{
    private ApplicationDbContext _context = null!;
    private IMediator _mediator = null!;
    private Permission _perm = null!;
    private static readonly DateTimeOffset T0 = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    [SetUp]
    public async Task SetUp()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"qc-audit-{Guid.NewGuid():N}")
            .Options;
        _context = new ApplicationDbContext(options);
        await _context.Database.EnsureCreatedAsync();

        _perm = Permission.Create("PERSONNEL.READ", "Personnel", "Read");
        _context.Permissions.Add(_perm);
        await _context.SaveChangesAsync();

        _mediator = BuildMediator();
    }

    [TearDown]
    public async Task TearDown()
    {
        await _context.Database.EnsureDeletedAsync();
        await _context.DisposeAsync();
    }

    [Test]
    public async Task Grant_Create_And_Revoke_Write_Audit_Entries()
    {
        var grantId = await _mediator.Send(new CreateGrantCommand(
            SubjectType.User, Guid.Empty, TestUsers.UserA, _perm.Id, null, null, ScopeKind.Unbounded, null,
            Effect.Allow, SourceType.User, Guid.Empty, T0, null, SourcePriority.IndividualOverride));

        await _mediator.Send(new RevokeGrantCommand(grantId, TestGuids.CompanyA));

        var created = await _context.AuthorizationAuditEntries.Where(x => x.EventType == "GrantCreated").ToListAsync();
        var revoked = await _context.AuthorizationAuditEntries.Where(x => x.EventType == "GrantRevoked").ToListAsync();

        created.Count.ShouldBe(1);
        revoked.Count.ShouldBe(1);
        revoked[0].ActorUserId.ShouldBe(TestGuids.CompanyA);
    }

    [Test]
    public async Task Delegation_Create_And_Revoke_Write_Audit_Entries()
    {
        _context.Grants.Add(Grant.CreateForUser(
            TestUsers.UserA, _perm.Id, SourceType.User, Guid.Empty, Effect.Allow, T0.AddDays(-1), null,
            SourcePriority.IndividualOverride));
        await _context.SaveChangesAsync();

        var delegationId = await _mediator.Send(new CreateDelegationCommand(TestUsers.UserA, TestUsers.UserB, _perm.Id, T0, null));
        await _mediator.Send(new RevokeDelegationCommand(delegationId));

        (await _context.AuthorizationAuditEntries.CountAsync(x => x.EventType == "DelegationCreated")).ShouldBe(1);
        (await _context.AuthorizationAuditEntries.CountAsync(x => x.EventType == "DelegationRevoked")).ShouldBe(1);
    }

    private IMediator BuildMediator()
    {
        var hierarchy = new PositionHierarchyService();
        var applicability = new GrantApplicabilityService(hierarchy);
        var engine = new AccessEvaluationEngine();

        return new ServiceCollection()
            .AddLogging()
            .AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<CreateGrantCommand>())
            .AddScoped<IApplicationDbContext>(_ => _context)
            .AddTestCurrentUser()
            .AddAuthorizationEvaluationServices()
            .BuildServiceProvider()
            .GetRequiredService<IMediator>();
    }
}
