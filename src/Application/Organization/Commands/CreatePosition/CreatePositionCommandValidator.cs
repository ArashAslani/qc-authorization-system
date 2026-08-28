namespace qc_authorization.Application.Organization.Commands.CreatePosition;

public class CreatePositionCommandValidator : AbstractValidator<CreatePositionCommand>
{
    public CreatePositionCommandValidator()
    {
        RuleFor(x => x.CompanyId).GreaterThan(0);
        RuleFor(x => x.Code).NotEmpty();
        RuleFor(x => x.Title).NotEmpty();

        When(x => x.ParentPositionId.HasValue, () =>
        {
            RuleFor(x => x.ParentPositionId!.Value).GreaterThan(0);
        });
    }
}
