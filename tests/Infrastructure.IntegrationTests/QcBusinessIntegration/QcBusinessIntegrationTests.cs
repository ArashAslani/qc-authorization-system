using qc_authorization.Application.Authorization.Audit;
using qc_authorization.Application.Authorization.Commands.CreateGrant;
using qc_authorization.Application.Authorization.Commands.CreatePermission;
using qc_authorization.Application.Authorization.Evaluation;
using qc_authorization.Application.Common.Interfaces;
using qc_authorization.Application.QcBusinessIntegration.Commands.ApproveControlPlan;
using qc_authorization.Application.QcBusinessIntegration.Commands.UpdateBom;
using qc_authorization.Application.QcBusinessIntegration.Commands.UpdateControlPlan;
using qc_authorization.Application.QcBusinessIntegration.Models;
using qc_authorization.Application.QcBusinessIntegration.Providers;
using qc_authorization.Domain.Authorization.Enums;
using qc_authorization.Domain.Authorization.Evaluation;
using qc_authorization.Domain.Authorization.Services;
using qc_authorization.Domain.Authorization.ValueObjects;
using qc_authorization.Domain.Organization;
using qc_authorization.Infrastructure.Data;
using qc_authorization.Infrastructure.IntegrationTests.TestSupport;
using qc_authorization.Tests.TestSupport;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Shouldly;

namespace qc_authorization.Infrastructure.IntegrationTests.QcBusinessIntegration;

public class InMemoryControlPlanStore : IControlPlanStore
{
    private readonly Dictionary<Guid, ControlPlan> _plans = new();

    public Task<ControlPlan?> FindByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _plans.TryGetValue(id, out var plan);
        return Task.FromResult(plan);
    }

    public Task SaveAsync(ControlPlan controlPlan, CancellationToken cancellationToken = default)
    {
        _plans[controlPlan.Id] = controlPlan;
        return Task.CompletedTask;
    }
}

public class InMemoryBomStore : IBomStore
{
    private readonly Dictionary<Guid, BOM> _boms = new();

    public Task<BOM?> FindByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _boms.TryGetValue(id, out var bom);
        return Task.FromResult(bom);
    }

    public Task SaveAsync(BOM bom, CancellationToken cancellationToken = default)
    {
        _boms[bom.Id] = bom;
        return Task.CompletedTask;
    }
}

[TestFixture]
public class QcBusinessIntegrationTests
{
    private ApplicationDbContext _context = null!;
    private IMediator _mediator = null!;
    private ServiceProvider _services = null!;
    private IControlPlanStore _controlPlanStore = null!;
    private IBomStore _bomStore = null!;

    [SetUp]
    public async Task SetUp()
    {
        var dbName = $"qc-biz-integration-{Guid.NewGuid():N}";
        _controlPlanStore = new InMemoryControlPlanStore();
        _bomStore = new InMemoryBomStore();

        _services = new ServiceCollection()
            .AddLogging()
            .AddDbContext<ApplicationDbContext>(options => options.UseInMemoryDatabase(dbName))
            .AddMediatR(cfg =>
            {
                cfg.RegisterServicesFromAssemblyContaining<CreatePermissionCommand>();
                cfg.RegisterServicesFromAssemblyContaining<ApproveControlPlanCommand>();
            })
            .AddTestCurrentUser()
            .AddAuthorizationEvaluationServices()
            .AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>())
            .AddSingleton(_controlPlanStore)
            .AddSingleton(_bomStore)
            .AddScoped<ControlPlanAuthorizationContextProvider>()
            .AddScoped<BomAuthorizationContextProvider>()
            .BuildServiceProvider();

        _context = _services.GetRequiredService<ApplicationDbContext>();
        await _context.Database.EnsureCreatedAsync();
        _mediator = _services.GetRequiredService<IMediator>();
    }

    [TearDown]
    public async Task TearDown()
    {
        await _context.Database.EnsureDeletedAsync();
        await _context.DisposeAsync();
        await _services.DisposeAsync();
    }

    [Test]
    public async Task HoldingManager_CanApprove_Across_Companies()
    {
        var cpApprovePermId = await _mediator.Send(new CreatePermissionCommand(
            "CONTROL_PLAN", "Control Plan", "APPROVE", "Approve"));

        var holdingManagerId = Guid.NewGuid();

        await _mediator.Send(new CreateGrantCommand(
            SubjectType.User,
            Guid.Empty,
            holdingManagerId,
            cpApprovePermId,
            "CONTROL_PLAN",
            null,
            ScopeKind.Unbounded,
            null,
            Effect.Allow,
            SourceType.User,
            Guid.Empty,
            DateTimeOffset.UtcNow.AddDays(-1),
            null,
            100));

        var planCompany10 = ControlPlan.Create(
            TestGuids.ControlPlan101, "CP-101", "Engine Control Plan",
            companyId: TestGuids.CompanyA, laboratoryId: TestGuids.Laboratory1,
            status: ControlPlanStatus.UnderReview);
        var planCompany20 = ControlPlan.Create(
            TestGuids.ControlPlan201, "CP-201", "Transmission Plan",
            companyId: TestGuids.CompanyB, laboratoryId: TestGuids.Laboratory2,
            status: ControlPlanStatus.UnderReview);
        await _controlPlanStore.SaveAsync(planCompany10);
        await _controlPlanStore.SaveAsync(planCompany20);

        var result1 = await _mediator.Send(new ApproveControlPlanCommand(TestGuids.ControlPlan101, holdingManagerId));
        var result2 = await _mediator.Send(new ApproveControlPlanCommand(TestGuids.ControlPlan201, holdingManagerId));

        result1.ShouldBeTrue();
        result2.ShouldBeTrue();

        (await _controlPlanStore.FindByIdAsync(TestGuids.ControlPlan101))!.Status.ShouldBe(ControlPlanStatus.Approved);
        (await _controlPlanStore.FindByIdAsync(TestGuids.ControlPlan201))!.Status.ShouldBe(ControlPlanStatus.Approved);
    }

    [Test]
    public async Task CompanyManager_CanApprove_OnlyWithin_CompanyScope()
    {
        var cpApprovePermId = await _mediator.Send(new CreatePermissionCommand(
            "CONTROL_PLAN", "Control Plan", "APPROVE", "Approve"));

        var company10ManagerId = Guid.NewGuid();

        await _mediator.Send(new CreateGrantCommand(
            SubjectType.User,
            Guid.Empty,
            company10ManagerId,
            cpApprovePermId,
            "CONTROL_PLAN",
            null,
            ScopeKind.Company,
            TestGuids.CompanyA.ToString(),
            Effect.Allow,
            SourceType.User,
            Guid.Empty,
            DateTimeOffset.UtcNow.AddDays(-1),
            null,
            100));

        var planInCompany10 = ControlPlan.Create(
            TestGuids.ControlPlan301, "CP-301", "Hydraulics Plan",
            companyId: TestGuids.CompanyA, laboratoryId: TestGuids.Laboratory1,
            status: ControlPlanStatus.UnderReview);
        var planInCompany20 = ControlPlan.Create(
            TestGuids.ControlPlan302, "CP-302", "Electronics Plan",
            companyId: TestGuids.CompanyB, laboratoryId: TestGuids.Laboratory2,
            status: ControlPlanStatus.UnderReview);
        await _controlPlanStore.SaveAsync(planInCompany10);
        await _controlPlanStore.SaveAsync(planInCompany20);

        var result = await _mediator.Send(new ApproveControlPlanCommand(TestGuids.ControlPlan301, company10ManagerId));
        result.ShouldBeTrue();
        (await _controlPlanStore.FindByIdAsync(TestGuids.ControlPlan301))!.Status.ShouldBe(ControlPlanStatus.Approved);

        var ex = Should.Throw<UnauthorizedAccessException>(async () =>
            await _mediator.Send(new ApproveControlPlanCommand(TestGuids.ControlPlan302, company10ManagerId)));
        ex.Message.ShouldContain("Access Denied");
    }

    [Test]
    public async Task BusinessRule_Enforced_Separately_From_Authorization()
    {
        var cpApprovePermId = await _mediator.Send(new CreatePermissionCommand(
            "CONTROL_PLAN", "Control Plan", "APPROVE", "Approve"));

        var managerId = Guid.NewGuid();

        await _mediator.Send(new CreateGrantCommand(
            SubjectType.User,
            Guid.Empty,
            managerId,
            cpApprovePermId,
            "CONTROL_PLAN",
            null,
            ScopeKind.Unbounded,
            null,
            Effect.Allow,
            SourceType.User,
            Guid.Empty,
            DateTimeOffset.UtcNow.AddDays(-1),
            null,
            100));

        var draftPlan = ControlPlan.Create(
            TestGuids.ControlPlan401, "CP-401", "Draft Plan",
            companyId: TestGuids.CompanyA, laboratoryId: TestGuids.Laboratory1,
            status: ControlPlanStatus.Draft);
        await _controlPlanStore.SaveAsync(draftPlan);

        var ex = Should.Throw<InvalidOperationException>(async () =>
            await _mediator.Send(new ApproveControlPlanCommand(TestGuids.ControlPlan401, managerId)));
        ex.Message.ShouldContain("Business Invariant Violation");
    }

    [Test]
    public async Task BomUpdate_Enforces_BOM_Authorization_Contract()
    {
        var bomUpdatePermId = await _mediator.Send(new CreatePermissionCommand(
            "BOM", "Bill of Materials", "UPDATE", "Update"));

        var engineerId = Guid.NewGuid();

        await _mediator.Send(new CreateGrantCommand(
            SubjectType.User,
            Guid.Empty,
            engineerId,
            bomUpdatePermId,
            "BOM",
            null,
            ScopeKind.Company,
            TestGuids.CompanyA.ToString(),
            Effect.Allow,
            SourceType.User,
            Guid.Empty,
            DateTimeOffset.UtcNow.AddDays(-1),
            null,
            100));

        var bom = BOM.Create(TestGuids.Bom501, "BOM-001", companyId: TestGuids.CompanyA, "1.0", "Initial BOM");
        await _bomStore.SaveAsync(bom);

        var result = await _mediator.Send(new UpdateBomCommand(TestGuids.Bom501, "Updated components", "1.1", engineerId));
        result.ShouldBeTrue();

        var updatedBom = await _bomStore.FindByIdAsync(TestGuids.Bom501);
        updatedBom!.Description.ShouldBe("Updated components");
        updatedBom.Revision.ShouldBe("1.1");
    }
}
