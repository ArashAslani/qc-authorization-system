namespace qc_authorization.Application.Authorization.Commands.RevokeDelegation;

public class RevokeDelegationCommandValidator : AbstractValidator<RevokeDelegationCommand>
{
    public RevokeDelegationCommandValidator()
    {
        RuleFor(x => x.DelegationId).GreaterThan(0);
    }
}
