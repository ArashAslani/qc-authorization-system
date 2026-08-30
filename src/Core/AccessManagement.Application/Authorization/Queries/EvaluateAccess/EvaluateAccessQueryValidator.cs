namespace AccessManagement.Application.Authorization.Queries.EvaluateAccess;

public class EvaluateAccessQueryValidator : AbstractValidator<EvaluateAccessQuery>
{
    public EvaluateAccessQueryValidator()
    {
        RuleFor(x => x.UserId).NotEqual(Guid.Empty);
        RuleFor(x => x.PermissionCode).NotEmpty();
    }
}
