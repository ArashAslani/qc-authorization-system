namespace qc_authorization.Application.Authorization.Commands.CreateRoleGroup;

public class CreateRoleGroupCommandValidator : AbstractValidator<CreateRoleGroupCommand>
{
    public CreateRoleGroupCommandValidator()
    {
        RuleFor(x => x.Code).NotEmpty();
        RuleFor(x => x.Name).NotEmpty();
    }
}
