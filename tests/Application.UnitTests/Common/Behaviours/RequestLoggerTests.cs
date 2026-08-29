using qc_authorization.Application.Common.Behaviours;
using qc_authorization.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace qc_authorization.Application.UnitTests.Common.Behaviours;

using qc_authorization.Tests.TestSupport;

public class RequestLoggerTests
{
    private Mock<ILogger<RequestLoggerTests.TestRequest>> _logger = null!;
    private Mock<ICurrentUser> _user = null!;

    [SetUp]
    public void Setup()
    {
        _logger = new Mock<ILogger<TestRequest>>();
        _user = new Mock<ICurrentUser>();
    }

    [Test]
    public async Task ShouldLogUserIdIfAuthenticated()
    {
        _user.Setup(x => x.UserId).Returns(Guid.NewGuid());

        var requestLogger = new LoggingBehaviour<TestRequest>(_logger.Object, _user.Object);

        await requestLogger.Process(new TestRequest(), new CancellationToken());
    }

    [Test]
    public async Task ShouldLogEmptyUserIdIfUnauthenticated()
    {
        var requestLogger = new LoggingBehaviour<TestRequest>(_logger.Object, _user.Object);

        await requestLogger.Process(new TestRequest(), new CancellationToken());
    }

    public class TestRequest { }
}
