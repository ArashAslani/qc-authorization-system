namespace qc_authorization.Application.Authorization.Commands.AddRoleToGroup;

public class AddRoleToGroupCommandValidator : AbstractValidator<AddRoleToGroupCommand>
{
    public AddRoleToGroupCommandValidator()
    {
        RuleFor(x => x.RoleGroupId).NotEqual(Guid.Empty);
        RuleFor(x => x.RoleId).NotEqual(Guid.Empty);
    }
}
