using AccessManagement.Application.Authorization.Services;
using AccessManagement.Application.Common.Exceptions;
using AccessManagement.Application.Common.Interfaces;
using AccessManagement.Application.Common.Security;

namespace AccessManagement.Application.Common.Behaviours;

public sealed class RequireUserAdminBehaviour<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly ICurrentUser _currentUser;
    private readonly IActorAccessService _actorAccess;

    public RequireUserAdminBehaviour(ICurrentUser currentUser, IActorAccessService actorAccess)
    {
        _currentUser = currentUser;
        _actorAccess = actorAccess;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (request is not IRequireUserAdmin)
        {
            return await next();
        }

        if (_currentUser.UserId is not Guid userId)
        {
            throw new UnauthorizedAccessException();
        }

        if (!await _actorAccess.IsUserAdminAsync(userId, _currentUser.ActiveCompanyId, cancellationToken))
        {
            throw new ForbiddenAccessException();
        }

        return await next();
    }
}
