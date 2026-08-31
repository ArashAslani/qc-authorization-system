using System.Reflection;
using AccessManagement.Application.Common.Exceptions;
using AccessManagement.Application.Common.Interfaces;
using AccessManagement.Application.Common.Security;

namespace AccessManagement.Application.Common.Behaviours;

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
        var authorizeAttributes = request.GetType().GetCustomAttributes<AuthorizeAttribute>().ToArray();

        if (authorizeAttributes.Length == 0)
        {
            return await next();
        }

        if (!_user.IsAuthenticated || _user.UserId is null)
        {
            throw new UnauthorizedAccessException();
        }

        foreach (var attribute in authorizeAttributes)
        {
            if (!string.IsNullOrWhiteSpace(attribute.Roles) || !string.IsNullOrWhiteSpace(attribute.Policy))
            {
                throw new ForbiddenAccessException(
                    "MediatR Roles/Policy attributes are not mapped to ASP.NET policies. Use IRequireUserAdmin.");
            }
        }

        return await next();
    }
}
