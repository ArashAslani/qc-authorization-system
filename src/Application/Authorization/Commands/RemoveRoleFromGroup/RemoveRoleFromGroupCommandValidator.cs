namespace qc_authorization.Application.Authorization.Commands.RemoveRoleFromGroup;

public class RemoveRoleFromGroupCommandValidator : AbstractValidator<RemoveRoleFromGroupCommand>
{
    public RemoveRoleFromGroupCommandValidator()
    {
        RuleFor(x => x.RoleGroupId).NotEqual(Guid.Empty);
        RuleFor(x => x.RoleId).NotEqual(Guid.Empty);
    }
}
