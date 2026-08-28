using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using qc_authorization.Application.Authorization.Audit;
using qc_authorization.Application.Authorization.Commands.CreateGrant;
using qc_authorization.Application.Common.Interfaces;
using qc_authorization.Domain.Authorization;
using qc_authorization.Domain.Authorization.Enums;
using qc_authorization.Domain.Authorization.ValueObjects;
using qc_authorization.Infrastructure.Data;
using qc_authorization.Infrastructure.IntegrationTests.TestSupport;
using MediatR;
using NUnit.Framework;
using Shouldly;

namespace qc_authorization.Infrastructure.IntegrationTests.Authorization;

[TestFixture]
public class CreateGrantCommandHandlerTests
{
    private ApplicationDbContext _context = null!;
    private IMediator _mediator = null!;

    [SetUp]
    public async Task SetUp()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"qc-grants-{Guid.NewGuid()}")
            .Options;
        _context = new ApplicationDbContext(options);
        await _context.Database.EnsureCreatedAsync();

        _mediator = new ServiceCollection()
            .AddLogging()
            .AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<CreateGrantCommand>())
            .AddScoped<IApplicationDbContext>(_ => _context)
            .AddScoped<IAuthorizationAuditService, AuthorizationAuditService>()
            .BuildServiceProvider()
            .GetRequiredService<IMediator>();

        _context.Permissions.Add(Permission.Create("PERSONNEL.READ", "Personnel", "Read"));
        await _context.SaveChangesAsync();
    }

    [TearDown]
    public async Task TearDown()
    {
        await _context.Database.EnsureDeletedAsync();
        await _context.DisposeAsync();
    }

    [Test]
    public async Task Can_Create_Allow_Grant_For_Role()
    {
        var permissionId = _context.Permissions.Single().Id;

        var id = await _mediator.Send(new CreateGrantCommand(
            SubjectType.Role,
            SubjectId: 50,
            SubjectUserId: null,
            PermissionId: permissionId,
            Resource: null,
            ResourceId: null,
            ScopeKind.Unbounded,
            ScopeIdentifier: null,
            Effect.Allow,
            SourceType.Role,
            SourceId: 50,
            DateTimeOffset.UtcNow,
            ValidTo: null,
            Priority: SourcePriority.RoleOrRoleGroup));

        var g = await _context.Grants.SingleAsync(x => x.Id == id);
        g.Effect.ShouldBe(Effect.Allow);
        g.SourceType.ShouldBe(SourceType.Role);
        g.SourceId.ShouldBe(50);
        g.SubjectType.ShouldBe(SubjectType.Role);
        g.SubjectId.ShouldBe(50);
        g.PermissionId.ShouldBe(permissionId);
        g.Priority.ShouldBe(SourcePriority.RoleOrRoleGroup);
    }

    [Test]
    public async Task Can_Create_Deny_Grant_For_Position()
    {
        var permissionId = _context.Permissions.Single().Id;

        var id = await _mediator.Send(new CreateGrantCommand(
            SubjectType.Position,
            SubjectId: 200,
            SubjectUserId: null,
            PermissionId: permissionId,
            Resource: "Personnel",
            ResourceId: null,
            ScopeKind.Company,
            ScopeIdentifier: "C-1",
            Effect.Deny,
            SourceType.Position,
            SourceId: 200,
            DateTimeOffset.UtcNow,
            ValidTo: null,
            Priority: SourcePriority.PositionOverride));

        var g = await _context.Grants.SingleAsync(x => x.Id == id);
        g.Effect.ShouldBe(Effect.Deny);
        g.ScopeKind.ShouldBe(ScopeKind.Company);
        g.ScopeIdentifier.ShouldBe("C-1");
        g.Priority.ShouldBe(SourcePriority.PositionOverride);
    }

    [Test]
    public async Task Can_Create_Grant_With_ValidityWindow()
    {
        var permissionId = _context.Permissions.Single().Id;
        var from = new DateTimeOffset(2026, 9, 1, 0, 0, 0, TimeSpan.Zero);
        var to = new DateTimeOffset(2026, 9, 7, 0, 0, 0, TimeSpan.Zero);

        var id = await _mediator.Send(new CreateGrantCommand(
            SubjectType.User,
            SubjectId: 0,
            SubjectUserId: TestUsers.UserA,
            PermissionId: permissionId,
            Resource: null,
            ResourceId: null,
            ScopeKind.Unbounded,
            ScopeIdentifier: null,
            Effect.Allow,
            SourceType.User,
            SourceId: 0,
            from,
            to,
            Priority: SourcePriority.IndividualOverride));

        var g = await _context.Grants.SingleAsync(x => x.Id == id);
        g.ValidFrom.ShouldBe(from);
        g.ValidTo.ShouldBe(to);
        g.SubjectUserId.ShouldBe(TestUsers.UserA);
    }

    [Test]
    public async Task Source_Traceability_Is_Preserved()
    {
        var permissionId = _context.Permissions.Single().Id;

        var id = await _mediator.Send(new CreateGrantCommand(
            SubjectType.RoleGroup,
            SubjectId: 999,
            SubjectUserId: null,
            PermissionId: permissionId,
            Resource: null,
            ResourceId: null,
            ScopeKind.Unbounded,
            ScopeIdentifier: null,
            Effect.Allow,
            SourceType.RoleGroup,
            SourceId: 999,
            DateTimeOffset.UtcNow,
            null,
            SourcePriority.RoleOrRoleGroup));

        var g = await _context.Grants.SingleAsync(x => x.Id == id);
        g.SourceType.ShouldBe(SourceType.RoleGroup);
        g.SourceId.ShouldBe(999);
    }
}
