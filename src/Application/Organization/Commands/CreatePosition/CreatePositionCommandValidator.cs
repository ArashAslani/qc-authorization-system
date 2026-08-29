namespace qc_authorization.Application.Organization.Commands.CreatePosition;

public class CreatePositionCommandValidator : AbstractValidator<CreatePositionCommand>
{
    public CreatePositionCommandValidator()
    {
        RuleFor(x => x.CompanyId).NotEqual(Guid.Empty);
        RuleFor(x => x.Code).NotEmpty();
        RuleFor(x => x.Title).NotEmpty();

        When(x => x.ParentPositionId.HasValue, () =>
        {
            RuleFor(x => x.ParentPositionId!.Value).NotEqual(Guid.Empty);
        });
    }
}
