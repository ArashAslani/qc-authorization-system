namespace qc_authorization.Application.Authorization.Commands.AssignRoleGroupToUser;

public class AssignRoleGroupToUserCommandValidator : AbstractValidator<AssignRoleGroupToUserCommand>
{
    public AssignRoleGroupToUserCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.RoleGroupId).NotEqual(Guid.Empty);

        RuleFor(x => x.ValidTo)
            .Must((cmd, validTo) => validTo is null || validTo >= cmd.ValidFrom)
            .WithMessage("ValidTo cannot be earlier than ValidFrom.");
    }
}
