namespace qc_authorization.Application.Authorization.Commands.RevokeRoleGroupFromUser;

public class RevokeRoleGroupFromUserCommandValidator : AbstractValidator<RevokeRoleGroupFromUserCommand>
{
    public RevokeRoleGroupFromUserCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.RoleGroupId).NotEqual(Guid.Empty);
    }
}
