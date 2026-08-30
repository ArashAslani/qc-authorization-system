namespace AccessManagement.Application.Organization.Commands.ReparentPosition;

public class ReparentPositionCommandValidator : AbstractValidator<ReparentPositionCommand>
{
    public ReparentPositionCommandValidator()
    {
        RuleFor(x => x.PositionId).NotEqual(Guid.Empty);

        When(x => x.NewParentPositionId.HasValue, () =>
        {
            RuleFor(x => x.NewParentPositionId!.Value).NotEqual(Guid.Empty);
            RuleFor(x => x.NewParentPositionId!.Value)
                .NotEqual(x => x.PositionId)
                .WithMessage("A position cannot be its own parent.");
        });
    }
}
