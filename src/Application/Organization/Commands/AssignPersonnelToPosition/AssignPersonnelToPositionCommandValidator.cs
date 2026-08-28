namespace qc_authorization.Application.Organization.Commands.AssignPersonnelToPosition;

public class AssignPersonnelToPositionCommandValidator : AbstractValidator<AssignPersonnelToPositionCommand>
{
    public AssignPersonnelToPositionCommandValidator()
    {
        RuleFor(x => x.PersonnelId).GreaterThan(0);
        RuleFor(x => x.PositionId).GreaterThan(0);

        RuleFor(x => x.ValidTo)
            .Must((cmd, validTo) => validTo is null || validTo >= cmd.ValidFrom)
            .WithMessage("ValidTo cannot be earlier than ValidFrom.");
    }
}
