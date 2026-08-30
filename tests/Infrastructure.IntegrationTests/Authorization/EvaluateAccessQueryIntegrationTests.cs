using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using AccessManagement.Application.Abstractions;
using AccessManagement.Application.Authorization.Queries.EvaluateAccess;
using AccessManagement.Application.Authorization.Evaluation;
using AccessManagement.Application.Common.Interfaces;
using AccessManagement.Application.Common.Mappings;
using AccessManagement.Domain.Authorization;
using AccessManagement.Domain.Authorization.Enums;
using AccessManagement.Domain.Authorization.Evaluation;
using AccessManagement.Domain.Authorization.Services;
using AccessManagement.Domain.Authorization.ValueObjects;
using AccessManagement.Domain.Organization;
using AccessManagement.Infrastructure.Data;
using AccessManagement.Infrastructure.IntegrationTests.TestSupport;
using MediatR;
using NUnit.Framework;
using Shouldly;

namespace AccessManagement.Infrastructure.IntegrationTests.Authorization;

using AccessManagement.Tests.TestSupport;

[TestFixture]
public class EvaluateAccessQueryIntegrationTests
{
    private ApplicationDbContext _context = null!;
    private IMediator _mediator = null!;
    private static readonly DateTimeOffset T0 = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    [SetUp]
    public async Task SetUp()
    {
        MappingConfig.RegisterMappings();

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"qc-eval-{Guid.NewGuid():N}")
            .Options;
        _context = new ApplicationDbContext(options);
        await _context.Database.EnsureCreatedAsync();

        var perm = Permission.Create("PERSONNEL.READ", "Personnel", "Read");
        _context.Permissions.Add(perm);
        await _context.SaveChangesAsync();
        _context.Grants.Add(Grant.CreateForUser(
            TestUsers.UserE, perm.Id, SourceType.User, Guid.Empty, Effect.Allow, T0.AddDays(-1), null,
            SourcePriority.IndividualOverride));
        await _context.SaveChangesAsync();

        _mediator = new ServiceCollection()
            .AddLogging()
            .AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<EvaluateAccessQuery>())
            .AddScoped<IApplicationDbContext>(_ => _context)
            .AddTestCurrentUser()
            .AddAuthorizationEvaluationServices()
            .BuildServiceProvider()
            .GetRequiredService<IMediator>();
    }

    [TearDown]
    public async Task TearDown()
    {
        await _context.Database.EnsureDeletedAsync();
        await _context.DisposeAsync();
    }

    [Test]
    public async Task EvaluateAccess_Returns_Allow_With_Trace()
    {
        var result = await _mediator.Send(new EvaluateAccessQuery(
            SubjectType.User, Guid.Empty, TestUsers.UserE, "Read", "Personnel", null, T0));

        result.Allowed.ShouldBeTrue();
    }

    [Test]
    public async Task EvaluateAccess_Returns_Deny_For_Unknown_User()
    {
        var result = await _mediator.Send(new EvaluateAccessQuery(
            SubjectType.User, Guid.Empty, TestUsers.Unknown, "Read", "Personnel", null, T0));

        result.Allowed.ShouldBeFalse();
    }
}
