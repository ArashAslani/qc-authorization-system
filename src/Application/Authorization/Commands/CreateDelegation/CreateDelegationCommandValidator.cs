namespace qc_authorization.Application.Authorization.Commands.CreateDelegation;

public class CreateDelegationCommandValidator : AbstractValidator<CreateDelegationCommand>
{
    public CreateDelegationCommandValidator()
    {
        RuleFor(x => x.DelegatorUserId).NotEmpty();
        RuleFor(x => x.DelegateUserId).NotEmpty();
        RuleFor(x => x.DelegatorUserId)
            .NotEqual(x => x.DelegateUserId)
            .WithMessage("A user cannot delegate to themselves.");

        RuleFor(x => x.PermissionId).NotEqual(Guid.Empty);

        When(x => x.ParentDelegationId.HasValue, () =>
        {
            RuleFor(x => x.ParentDelegationId!.Value).NotEqual(Guid.Empty);
        });

        RuleFor(x => x.ValidTo)
            .Must((cmd, validTo) => validTo is null || validTo >= cmd.ValidFrom)
            .WithMessage("ValidTo cannot be earlier than ValidFrom.");
    }
}
