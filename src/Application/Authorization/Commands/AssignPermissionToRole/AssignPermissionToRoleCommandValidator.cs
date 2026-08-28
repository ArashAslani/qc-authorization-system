namespace qc_authorization.Application.Authorization.Commands.AssignPermissionToRole;

public class AssignPermissionToRoleCommandValidator : AbstractValidator<AssignPermissionToRoleCommand>
{
    public AssignPermissionToRoleCommandValidator()
    {
        RuleFor(x => x.RoleId).GreaterThan(0);
        RuleFor(x => x.PermissionId).GreaterThan(0);
    }
}
