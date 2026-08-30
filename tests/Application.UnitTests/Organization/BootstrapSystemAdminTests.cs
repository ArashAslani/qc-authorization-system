using AccessManagement.Application.Organization.Commands.BootstrapSystemAdmin;
using AccessManagement.Application.UnitTests.TestSupport;
using AccessManagement.Domain.Authorization.Exceptions;
using AccessManagement.Infrastructure.Data;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Shouldly;

namespace AccessManagement.Application.UnitTests.Organization;

[TestFixture]
public class BootstrapSystemAdminTests
{
    private ApplicationDbContext _db = null!;
    private IMediator _mediator = null!;

    [SetUp]
    public async Task SetUp()
    {
        (_db, _) = AuthorizationTestContext.Create();
        await _db.Database.EnsureCreatedAsync();
        _mediator = AuthorizationTestContext.CreateMediatorServices(_db, userId: null)
            .GetRequiredService<IMediator>();
    }

    [TearDown]
    public async Task TearDown()
    {
        await _db.Database.EnsureDeletedAsync();
        await _db.DisposeAsync();
    }

    [Test]
    public async Task Bootstrap_Succeeds_On_Empty_Database()
    {
        var identityUserId = Guid.NewGuid();

        var id = await _mediator.Send(new BootstrapSystemAdminCommand(
            "001", "Ada", "Admin", "A-1", identityUserId));

        id.ShouldNotBe(Guid.Empty);
        var personnel = await _db.Personnel.FindAsync(id);
        personnel.ShouldNotBeNull();
        personnel.IsSystemUser.ShouldBeTrue();
        personnel.IdentityUserId.ShouldBe(identityUserId);
        personnel.FirstName.ShouldBe("Ada");
        personnel.LastName.ShouldBe("Admin");
        personnel.PersonnelCode.ShouldBe("A-1");
    }

    [Test]
    public async Task Bootstrap_Fails_When_Admin_Already_Exists()
    {
        await _mediator.Send(new BootstrapSystemAdminCommand(
            "001", "Ada", "Admin", "A-1", Guid.NewGuid()));

        var ex = await Should.ThrowAsync<AuthorizationDomainException>(() =>
            _mediator.Send(new BootstrapSystemAdminCommand(
                "002", "Other", "Admin", "A-2", Guid.NewGuid())));

        ex.Message.ShouldContain("Bootstrap is disabled");
        _db.Personnel.Count(p => p.IsSystemUser).ShouldBe(1);
    }
}
