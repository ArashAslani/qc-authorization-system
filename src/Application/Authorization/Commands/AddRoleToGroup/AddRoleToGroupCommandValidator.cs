namespace qc_authorization.Application.Authorization.Commands.AddRoleToGroup;

public class AddRoleToGroupCommandValidator : AbstractValidator<AddRoleToGroupCommand>
{
    public AddRoleToGroupCommandValidator()
    {
        RuleFor(x => x.RoleGroupId).GreaterThan(0);
        RuleFor(x => x.RoleId).GreaterThan(0);
    }
}
