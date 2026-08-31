using AccessManagement.Application.Authorization.Queries.EvaluateAccess;
using AccessManagement.Application.Common.Exceptions;
using AccessManagement.Application.UnitTests.TestSupport;
using AccessManagement.Infrastructure.Data;
using AccessManagement.Tests.TestSupport;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Shouldly;

namespace AccessManagement.Application.UnitTests.Authorization;

[TestFixture]
public class CompanyBoundaryEvaluationTests
{
    private ApplicationDbContext _db = null!;

    [SetUp]
    public async Task SetUp()
    {
        (_db, _) = AuthorizationTestContext.Create();
        await _db.Database.EnsureCreatedAsync();
    }

    [TearDown]
    public async Task TearDown()
    {
        await _db.Database.EnsureDeletedAsync();
        await _db.DisposeAsync();
    }

    [Test]
    public async Task EvaluateAccess_Without_Active_Company_Is_Forbidden()
    {
        var mediator = AuthorizationTestContext.CreateMediatorServices(_db, userId: TestUsers.UserA, hasCompany: false)
            .GetRequiredService<IMediator>();

        await Should.ThrowAsync<ForbiddenAccessException>(() =>
            mediator.Send(new EvaluateAccessQuery(TestUsers.UserA, "PERSONNEL.READ")));
    }
}
