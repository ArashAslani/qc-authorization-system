namespace AccessManagement.Application.Authorization.Commands.AssignAuthorizationRoleToUser;

public class AssignAuthorizationRoleToUserCommandValidator : AbstractValidator<AssignAuthorizationRoleToUserCommand>
{
    public AssignAuthorizationRoleToUserCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.RoleId).NotEqual(Guid.Empty);

        RuleFor(x => x.ValidTo)
            .Must((cmd, validTo) => validTo is null || validTo >= cmd.ValidFrom)
            .WithMessage("ValidTo cannot be earlier than ValidFrom.");
    }
}
