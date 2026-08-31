using AccessManagement.Application.Authorization.Services;
using AccessManagement.Application.Common.Interfaces;
using AccessManagement.Application.UnitTests.TestSupport;
using AccessManagement.Infrastructure.Data;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Shouldly;

namespace AccessManagement.Application.UnitTests.Authorization;

[TestFixture]
public class CompanyVisibilityFailClosedTests
{
    private ApplicationDbContext _db = null!;
    private ICompanyVisibilityService _service = null!;

    [SetUp]
    public async Task SetUp()
    {
        (_db, _) = AuthorizationTestContext.Create();
        await _db.Database.EnsureCreatedAsync();
        _service = BuildService(new StaticCurrentUser(userId: null));
    }

    [TearDown]
    public async Task TearDown()
    {
        await _db.Database.EnsureDeletedAsync();
        await _db.DisposeAsync();
    }

    [Test]
    public async Task ResolveAsync_Throws_When_No_Authenticated_User()
    {
        await Should.ThrowAsync<UnauthorizedAccessException>(() => _service.ResolveAsync());
    }

    [Test]
    public async Task EnsureAuditReaderAsync_Throws_When_No_Authenticated_User()
    {
        await Should.ThrowAsync<UnauthorizedAccessException>(() => _service.EnsureAuditReaderAsync());
    }

    [Test]
    public async Task IsAdminAsync_Returns_False_When_No_Authenticated_User()
    {
        (await _service.IsAdminAsync()).ShouldBeFalse();
    }

    private ICompanyVisibilityService BuildService(ICurrentUser currentUser)
    {
        var services = AuthorizationTestContext.CreateMediatorServices(_db, userId: null);
        return ActivatorUtilities.CreateInstance<CompanyVisibilityService>(services, currentUser);
    }
}
