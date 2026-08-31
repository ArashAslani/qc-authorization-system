using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using AccessManagement.Application.Authorization.Audit;
using AccessManagement.Application.Authorization.Commands.CreateGrant;
using AccessManagement.Application.Common.Interfaces;
using AccessManagement.Domain.Authorization;
using AccessManagement.Domain.Authorization.Enums;
using AccessManagement.Domain.Authorization.ValueObjects;
using AccessManagement.Infrastructure.Data;
using AccessManagement.Infrastructure.IntegrationTests.TestSupport;
using MediatR;
using NUnit.Framework;
using Shouldly;

namespace AccessManagement.Infrastructure.IntegrationTests.Authorization;

using AccessManagement.Tests.TestSupport;

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
        await _context.SeedTestAdminAsync();

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
            SubjectId: TestGuids.Subject50,
            SubjectUserId: null,
            PermissionId: permissionId,
            Resource: null,
            ResourceId: null,
            ScopeUnitId: null,
            Effect.Allow,
            SourceType.Role,
            SourceId: TestGuids.Subject50,
            DateTimeOffset.UtcNow,
            ValidTo: null,
            Priority: SourcePriority.RoleOrRoleGroup));

        var g = await _context.Grants.SingleAsync(x => x.Id == id);
        g.Effect.ShouldBe(Effect.Allow);
        g.SourceType.ShouldBe(SourceType.Role);
        g.SourceId.ShouldBe(TestGuids.Subject50);
        g.SubjectType.ShouldBe(SubjectType.Role);
        g.SubjectId.ShouldBe(TestGuids.Subject50);
        g.PermissionId.ShouldBe(permissionId);
        g.Priority.ShouldBe(SourcePriority.RoleOrRoleGroup);
    }

    [Test]
    public async Task Can_Create_Deny_Grant_For_Position()
    {
        var permissionId = _context.Permissions.Single().Id;

        var id = await _mediator.Send(new CreateGrantCommand(
            SubjectType.Position,
            SubjectId: TestGuids.Subject50,
            SubjectUserId: null,
            PermissionId: permissionId,
            Resource: "Personnel",
            ResourceId: null,
            ScopeUnitId: TestGuids.CompanyA,
            Effect.Deny,
            SourceType.Position,
            SourceId: TestGuids.Subject50,
            DateTimeOffset.UtcNow,
            ValidTo: null,
            Priority: SourcePriority.PositionOverride));

        var g = await _context.Grants.SingleAsync(x => x.Id == id);
        g.Effect.ShouldBe(Effect.Deny);
        g.ScopeUnitId.ShouldBe(TestGuids.CompanyA);
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
            SubjectId: Guid.Empty,
            SubjectUserId: TestUsers.UserA,
            PermissionId: permissionId,
            Resource: null,
            ResourceId: null,
            ScopeUnitId: null,
            Effect.Allow,
            SourceType.User,
            SourceId: Guid.Empty,
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
            SubjectId: TestGuids.Subject50,
            SubjectUserId: null,
            PermissionId: permissionId,
            Resource: null,
            ResourceId: null,
            ScopeUnitId: null,
            Effect.Allow,
            SourceType.RoleGroup,
            SourceId: TestGuids.Subject50,
            DateTimeOffset.UtcNow,
            null,
            SourcePriority.RoleOrRoleGroup));

        var g = await _context.Grants.SingleAsync(x => x.Id == id);
        g.SourceType.ShouldBe(SourceType.RoleGroup);
        g.SourceId.ShouldBe(TestGuids.Subject50);
    }
}
