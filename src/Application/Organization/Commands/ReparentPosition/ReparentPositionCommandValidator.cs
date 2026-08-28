namespace qc_authorization.Application.Organization.Commands.ReparentPosition;

public class ReparentPositionCommandValidator : AbstractValidator<ReparentPositionCommand>
{
    public ReparentPositionCommandValidator()
    {
        RuleFor(x => x.PositionId).GreaterThan(0);

        When(x => x.NewParentPositionId.HasValue, () =>
        {
            RuleFor(x => x.NewParentPositionId!.Value).GreaterThan(0);
            RuleFor(x => x.NewParentPositionId!.Value)
                .NotEqual(x => x.PositionId)
                .WithMessage("A position cannot be its own parent.");
        });
    }
}
