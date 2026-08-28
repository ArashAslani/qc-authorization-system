using qc_authorization.Application.Common.Behaviours;
using qc_authorization.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace qc_authorization.Application.UnitTests.Common.Behaviours;

public class RequestLoggerTests
{
    private Mock<ILogger<RequestLoggerTests.TestRequest>> _logger = null!;
    private Mock<IUser> _user = null!;

    [SetUp]
    public void Setup()
    {
        _logger = new Mock<ILogger<TestRequest>>();
        _user = new Mock<IUser>();
    }

    [Test]
    public async Task ShouldLogUserIdIfAuthenticated()
    {
        _user.Setup(x => x.Id).Returns(Guid.NewGuid().ToString());

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
