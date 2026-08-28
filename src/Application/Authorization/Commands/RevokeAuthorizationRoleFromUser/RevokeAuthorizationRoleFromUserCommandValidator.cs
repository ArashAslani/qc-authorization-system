namespace qc_authorization.Application.Authorization.Commands.RevokeAuthorizationRoleFromUser;

public class RevokeAuthorizationRoleFromUserCommandValidator : AbstractValidator<RevokeAuthorizationRoleFromUserCommand>
{
    public RevokeAuthorizationRoleFromUserCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.RoleId).GreaterThan(0);
    }
}
