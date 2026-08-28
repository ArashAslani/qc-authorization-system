namespace qc_authorization.Application.Authorization.Queries.EvaluateAccessForSubject;

public class EvaluateAccessForSubjectQueryValidator : AbstractValidator<EvaluateAccessForSubjectQuery>
{
    public EvaluateAccessForSubjectQueryValidator()
    {
        RuleFor(x => x.Action).NotEmpty();
        RuleFor(x => x.Resource).NotEmpty();
        RuleFor(x => x.When).NotEmpty();

        When(x => x.SubjectType == Domain.Authorization.Enums.SubjectType.User, () =>
        {
            RuleFor(x => x.UserId).NotNull().NotEmpty();
        });

        When(x => x.SubjectType != Domain.Authorization.Enums.SubjectType.User, () =>
        {
            RuleFor(x => x.SubjectId).GreaterThan(0);
        });
    }
}
