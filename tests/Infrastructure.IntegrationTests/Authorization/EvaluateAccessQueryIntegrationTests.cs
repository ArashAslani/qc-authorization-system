using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using qc_authorization.Application.Authorization.Queries.EvaluateAccess;
using qc_authorization.Application.Authorization.Evaluation;
using qc_authorization.Application.Common.Interfaces;
using qc_authorization.Application.Common.Mappings;
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

        var hierarchy = new PositionHierarchyService();
        var applicability = new GrantApplicabilityService(hierarchy);

        _mediator = new ServiceCollection()
            .AddLogging()
            .AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<EvaluateAccessQuery>())
            .AddSingleton(hierarchy)
            .AddSingleton(applicability)
            .AddSingleton(new AccessEvaluationEngine())
            .AddScoped<IApplicationDbContext>(_ => _context)
            .AddSingleton<ICurrentUser>(new StaticCurrentUser(activeCompanyId: TestGuids.CompanyA))
            .AddScoped<ICandidateGrantResolver, PositionAwareCandidateGrantResolver>()
            .AddScoped<IAccessEvaluator, AccessEvaluator>()
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

        result.Effect.ShouldBe("Allow");
        result.Trace.CandidateCount.ShouldBeGreaterThan(0);
    }

    [Test]
    public async Task EvaluateAccess_Returns_Deny_For_Unknown_User()
    {
        var result = await _mediator.Send(new EvaluateAccessQuery(
            SubjectType.User, Guid.Empty, TestUsers.Unknown, "Read", "Personnel", null, T0));

        result.Effect.ShouldBe("Deny");
    }
}
