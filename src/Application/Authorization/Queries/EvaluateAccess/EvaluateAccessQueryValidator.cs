using qc_authorization.Domain.Authorization.Enums;

namespace qc_authorization.Application.Authorization.Queries.EvaluateAccess;

public class EvaluateAccessQueryValidator : AbstractValidator<EvaluateAccessQuery>
{
    public EvaluateAccessQueryValidator()
    {
        When(x => x.SubjectType != SubjectType.User, () =>
        {
            RuleFor(x => x.SubjectId).NotEqual(Guid.Empty);
        });

        When(x => x.SubjectType == SubjectType.User, () =>
        {
            RuleFor(x => x.UserId)
                .NotNull()
                .Must(id => id != Guid.Empty)
                .WithMessage("UserId is required for user access evaluation.");
        });

        RuleFor(x => x.Action).NotEmpty();
        RuleFor(x => x.Resource).NotEmpty();
    }
}
