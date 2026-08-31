using AccessManagement.Application.Common.Interfaces;
using MediatR.Pipeline;
using Microsoft.Extensions.Logging;

namespace AccessManagement.Application.Common.Behaviours;

public class LoggingBehaviour<TRequest> : IRequestPreProcessor<TRequest>
    where TRequest : notnull
{
    private readonly ILogger _logger;
    private readonly ICurrentUser _user;

    public LoggingBehaviour(ILogger<TRequest> logger, ICurrentUser user)
    {
        _logger = logger;
        _user = user;
    }

    public Task Process(TRequest request, CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        var userId = _user.UserId?.ToString() ?? string.Empty;

        _logger.LogInformation("qc_authorization Request: {Name} {UserId}",
            requestName, userId);

        return Task.CompletedTask;
    }
}
