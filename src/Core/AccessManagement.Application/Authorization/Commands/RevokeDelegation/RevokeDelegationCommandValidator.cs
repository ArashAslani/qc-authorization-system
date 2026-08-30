namespace AccessManagement.Application.Authorization.Commands.RevokeDelegation;

public class RevokeDelegationCommandValidator : AbstractValidator<RevokeDelegationCommand>
{
    public RevokeDelegationCommandValidator()
    {
        RuleFor(x => x.DelegationId).NotEqual(Guid.Empty);
    }
}
