namespace qc_authorization.Application.Authorization.Commands.RemovePermissionFromRole;

public class RemovePermissionFromRoleCommandValidator : AbstractValidator<RemovePermissionFromRoleCommand>
{
    public RemovePermissionFromRoleCommandValidator()
    {
        RuleFor(x => x.RoleId).NotEqual(Guid.Empty);
        RuleFor(x => x.PermissionId).NotEqual(Guid.Empty);
    }
}
