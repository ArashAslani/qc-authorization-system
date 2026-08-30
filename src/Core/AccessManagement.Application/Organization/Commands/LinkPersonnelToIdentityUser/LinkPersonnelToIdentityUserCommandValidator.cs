namespace AccessManagement.Application.Organization.Commands.LinkPersonnelToIdentityUser;

public class LinkPersonnelToIdentityUserCommandValidator : AbstractValidator<LinkPersonnelToIdentityUserCommand>
{
    public LinkPersonnelToIdentityUserCommandValidator()
    {
        RuleFor(x => x.PersonnelId).NotEqual(Guid.Empty);
        RuleFor(x => x.IdentityUserId).NotEmpty();
    }
}
