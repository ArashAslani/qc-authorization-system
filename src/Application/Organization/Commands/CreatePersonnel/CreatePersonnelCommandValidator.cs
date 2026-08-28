namespace qc_authorization.Application.Organization.Commands.CreatePersonnel;

public class CreatePersonnelCommandValidator : AbstractValidator<CreatePersonnelCommand>
{
    public CreatePersonnelCommandValidator()
    {
        RuleFor(x => x.NationalId).NotEmpty();
        RuleFor(x => x.FirstName).NotEmpty();
        RuleFor(x => x.LastName).NotEmpty();
        RuleFor(x => x.PersonalCode).NotEmpty();

        When(x => x.IdentityUserId.HasValue, () =>
        {
            RuleFor(x => x.IdentityUserId!.Value).NotEmpty();
        });
    }
}
