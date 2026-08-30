namespace AccessManagement.Application.Authorization.Commands.AssignRoleGroupToPosition;

public class AssignRoleGroupToPositionCommandValidator : AbstractValidator<AssignRoleGroupToPositionCommand>
{
    public AssignRoleGroupToPositionCommandValidator()
    {
        RuleFor(x => x.PositionId).NotEqual(Guid.Empty);
        RuleFor(x => x.RoleGroupId).NotEqual(Guid.Empty);

        RuleFor(x => x.ValidTo)
            .Must((cmd, validTo) => validTo is null || validTo >= cmd.ValidFrom)
            .WithMessage("ValidTo cannot be earlier than ValidFrom.");
    }
}
