using qc_authorization.Domain.Authorization.Enums;

namespace qc_authorization.Application.Authorization.Commands.CreateGrant;

public class CreateGrantCommandValidator : AbstractValidator<CreateGrantCommand>
{
    public CreateGrantCommandValidator()
    {
        RuleFor(x => x.PermissionId).NotEqual(Guid.Empty);
        RuleFor(x => x.SourceId).NotEqual(Guid.Empty);

        When(x => x.SubjectType != SubjectType.User, () =>
        {
            RuleFor(x => x.SubjectId).NotEqual(Guid.Empty);
        });

        When(x => x.SubjectType == SubjectType.User, () =>
        {
            RuleFor(x => x.SubjectUserId)
                .NotNull()
                .Must(id => id != Guid.Empty)
                .WithMessage("SubjectUserId is required for user grants.");
        });

        RuleFor(x => x.ValidTo)
            .Must((cmd, validTo) => validTo is null || validTo >= cmd.ValidFrom)
            .WithMessage("ValidTo cannot be earlier than ValidFrom.");
    }
}
