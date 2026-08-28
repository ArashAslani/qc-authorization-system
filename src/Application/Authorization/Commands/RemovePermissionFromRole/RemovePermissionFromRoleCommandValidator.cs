namespace qc_authorization.Application.Authorization.Commands.RemovePermissionFromRole;

public class RemovePermissionFromRoleCommandValidator : AbstractValidator<RemovePermissionFromRoleCommand>
{
    public RemovePermissionFromRoleCommandValidator()
    {
        RuleFor(x => x.RoleId).GreaterThan(0);
        RuleFor(x => x.PermissionId).GreaterThan(0);
    }
}
