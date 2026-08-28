using System.Reflection;
using qc_authorization.Application.Common.Exceptions;
using qc_authorization.Application.Common.Interfaces;
using qc_authorization.Application.Common.Security;

namespace qc_authorization.Application.Common.Behaviours;

public class AuthorizationBehaviour<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly ICurrentUser _user;

    public AuthorizationBehaviour(ICurrentUser user)
    {
        _user = user;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var authorizeAttributes = request.GetType().GetCustomAttributes<AuthorizeAttribute>();

        if (authorizeAttributes.Any())
        {
            if (!_user.IsAuthenticated || _user.UserId is null)
            {
                throw new UnauthorizedAccessException();
            }
        }

        return await next();
    }
}
