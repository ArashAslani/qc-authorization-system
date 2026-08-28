namespace qc_authorization.Application.Authorization.Commands.RemoveRoleFromGroup;

public class RemoveRoleFromGroupCommandValidator : AbstractValidator<RemoveRoleFromGroupCommand>
{
    public RemoveRoleFromGroupCommandValidator()
    {
        RuleFor(x => x.RoleGroupId).GreaterThan(0);
        RuleFor(x => x.RoleId).GreaterThan(0);
    }
}
