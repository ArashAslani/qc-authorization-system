namespace qc_authorization.Application.Authorization.Commands.AssignAuthorizationRoleToUser;

public class AssignAuthorizationRoleToUserCommandValidator : AbstractValidator<AssignAuthorizationRoleToUserCommand>
{
    public AssignAuthorizationRoleToUserCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.RoleId).GreaterThan(0);

        RuleFor(x => x.ValidTo)
            .Must((cmd, validTo) => validTo is null || validTo >= cmd.ValidFrom)
            .WithMessage("ValidTo cannot be earlier than ValidFrom.");
    }
}
