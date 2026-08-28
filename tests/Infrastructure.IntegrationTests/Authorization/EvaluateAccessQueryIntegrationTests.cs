using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using qc_authorization.Application.Authorization.Commands.CreateGrant;
using qc_authorization.Application.Authorization.Queries.EvaluateAccess;
using qc_authorization.Application.Authorization.Evaluation;
using qc_authorization.Application.Common.Interfaces;
using qc_authorization.Application.Common.Interfaces.Repositories;
using qc_authorization.Domain.Authorization;
using qc_authorization.Domain.Authorization.Enums;
using qc_authorization.Domain.Authorization.Evaluation;
using qc_authorization.Domain.Authorization.Services;
using qc_authorization.Domain.Authorization.ValueObjects;
using qc_authorization.Domain.Organization;
using qc_authorization.Infrastructure.Data;
using qc_authorization.Infrastructure.Data.Repositories;
using MediatR;
using NUnit.Framework;
using Shouldly;

namespace qc_authorization.Infrastructure.IntegrationTests.Authorization;

[TestFixture]
public class EvaluateAccessQueryIntegrationTests
{
    private ApplicationDbContext _context = null!;
    private IMediator _mediator = null!;
    private static readonly DateTimeOffset T0 = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    [SetUp]
    public async Task SetUp()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"qc-eval-{Guid.NewGuid():N}")
            .Options;
        _context = new ApplicationDbContext(options);
        await _context.Database.EnsureCreatedAsync();

        var perm = Permission.Create("PERSONNEL.READ", "Personnel", "Read");
        _context.Permissions.Add(perm);
        _context.Grants.Add(Grant.Create(
            SubjectType.User, 42, perm.Id, SourceType.User, 42, Effect.Allow, T0.AddDays(-1), null,
            SourcePriority.IndividualOverride));
        await _context.SaveChangesAsync();

        var hierarchy = new PositionHierarchyService();
        var applicability = new GrantApplicabilityService(hierarchy);

        _mediator = new ServiceCollection()
            .AddLogging()
            .AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<EvaluateAccessQuery>())
            .AddSingleton(hierarchy)
            .AddSingleton(applicability)
            .AddSingleton(new Domain.Authorization.Evaluation.AccessEvaluationEngine())
            .AddScoped<IUnitOfWork>(_ => new UnitOfWork(_context))
            .AddScoped<IGrantRepository>(_ => new GrantRepository(_context))
            .AddScoped<IPermissionRepository>(_ => new PermissionRepository(_context))
            .AddScoped<IPositionRepository>(_ => new PositionRepository(_context))
            .AddScoped<IPositionAssignmentRepository>(_ => new PositionAssignmentRepository(_context))
            .AddScoped<IDelegationRepository>(_ => new DelegationRepository(_context))
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
            SubjectType.User, 42, "Read", "Personnel", null, T0));

        result.Effect.ShouldBe("Allow");
        result.Trace.CandidateCount.ShouldBeGreaterThan(0);
    }

    [Test]
    public async Task EvaluateAccess_Returns_Deny_For_Unknown_User()
    {
        var result = await _mediator.Send(new EvaluateAccessQuery(
            SubjectType.User, 99, "Read", "Personnel", null, T0));

        result.Effect.ShouldBe("Deny");
    }
}
