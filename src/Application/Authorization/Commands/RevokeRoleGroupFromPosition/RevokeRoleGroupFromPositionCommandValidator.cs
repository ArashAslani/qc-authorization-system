namespace qc_authorization.Application.Authorization.Commands.RevokeRoleGroupFromPosition;

public class RevokeRoleGroupFromPositionCommandValidator : AbstractValidator<RevokeRoleGroupFromPositionCommand>
{
    public RevokeRoleGroupFromPositionCommandValidator()
    {
        RuleFor(x => x.PositionId).NotEqual(Guid.Empty);
        RuleFor(x => x.RoleGroupId).NotEqual(Guid.Empty);
    }
}
