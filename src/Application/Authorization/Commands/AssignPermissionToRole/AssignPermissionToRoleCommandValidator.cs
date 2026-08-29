namespace qc_authorization.Application.Authorization.Commands.AssignPermissionToRole;

public class AssignPermissionToRoleCommandValidator : AbstractValidator<AssignPermissionToRoleCommand>
{
    public AssignPermissionToRoleCommandValidator()
    {
        RuleFor(x => x.RoleId).NotEqual(Guid.Empty);
        RuleFor(x => x.PermissionId).NotEqual(Guid.Empty);
    }
}
