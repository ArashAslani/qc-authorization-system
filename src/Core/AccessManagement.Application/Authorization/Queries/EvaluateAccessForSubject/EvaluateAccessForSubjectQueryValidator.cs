namespace AccessManagement.Application.Authorization.Queries.EvaluateAccessForSubject;

public class EvaluateAccessForSubjectQueryValidator : AbstractValidator<EvaluateAccessForSubjectQuery>
{
    public EvaluateAccessForSubjectQueryValidator()
    {
        RuleFor(x => x.UserId).NotEqual(Guid.Empty);
        RuleFor(x => x.PermissionCode).NotEmpty();
    }
}
