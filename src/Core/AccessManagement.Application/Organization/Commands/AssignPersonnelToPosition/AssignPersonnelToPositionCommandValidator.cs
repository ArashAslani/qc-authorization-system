namespace AccessManagement.Application.Organization.Commands.AssignPersonnelToPosition;

public class AssignPersonnelToPositionCommandValidator : AbstractValidator<AssignPersonnelToPositionCommand>
{
    public AssignPersonnelToPositionCommandValidator()
    {
        RuleFor(x => x.PersonnelId).NotEqual(Guid.Empty);
        RuleFor(x => x.PositionId).NotEqual(Guid.Empty);

        RuleFor(x => x.ValidTo)
            .Must((cmd, validTo) => validTo is null || validTo >= cmd.ValidFrom)
            .WithMessage("ValidTo cannot be earlier than ValidFrom.");
    }
}
