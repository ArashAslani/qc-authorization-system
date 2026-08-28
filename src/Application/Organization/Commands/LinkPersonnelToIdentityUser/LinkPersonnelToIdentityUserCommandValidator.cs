namespace qc_authorization.Application.Organization.Commands.LinkPersonnelToIdentityUser;

public class LinkPersonnelToIdentityUserCommandValidator : AbstractValidator<LinkPersonnelToIdentityUserCommand>
{
    public LinkPersonnelToIdentityUserCommandValidator()
    {
        RuleFor(x => x.PersonnelId).GreaterThan(0);
        RuleFor(x => x.IdentityUserId).NotEmpty();
    }
}
