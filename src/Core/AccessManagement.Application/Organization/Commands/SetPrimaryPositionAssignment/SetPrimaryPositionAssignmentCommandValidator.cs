namespace AccessManagement.Application.Organization.Commands.SetPrimaryPositionAssignment;

public class SetPrimaryPositionAssignmentCommandValidator : AbstractValidator<SetPrimaryPositionAssignmentCommand>
{
    public SetPrimaryPositionAssignmentCommandValidator()
    {
        RuleFor(x => x.PersonnelId).NotEqual(Guid.Empty);
        RuleFor(x => x.AssignmentId).NotEqual(Guid.Empty);
    }
}
