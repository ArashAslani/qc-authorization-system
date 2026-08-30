namespace AccessManagement.Application.Organization.Commands.BootstrapSystemAdmin;

public class BootstrapSystemAdminCommandValidator : AbstractValidator<BootstrapSystemAdminCommand>
{
    public BootstrapSystemAdminCommandValidator()
    {
        RuleFor(x => x.FirstName).NotEmpty();
        RuleFor(x => x.LastName).NotEmpty();
        RuleFor(x => x.IdentityUserId).NotEmpty();
    }
}
