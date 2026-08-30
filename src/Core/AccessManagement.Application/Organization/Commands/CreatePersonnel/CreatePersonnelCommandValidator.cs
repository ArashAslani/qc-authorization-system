namespace AccessManagement.Application.Organization.Commands.CreatePersonnel;

public class CreatePersonnelCommandValidator : AbstractValidator<CreatePersonnelCommand>
{
    public CreatePersonnelCommandValidator()
    {
        RuleFor(x => x.FirstName).NotEmpty();
        RuleFor(x => x.LastName).NotEmpty();

        When(x => x.IdentityUserId.HasValue, () =>
        {
            RuleFor(x => x.IdentityUserId!.Value).NotEmpty();
        });
    }
}
