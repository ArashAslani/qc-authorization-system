namespace AccessManagement.Application.Authorization.Commands.CreatePermission;

public class CreatePermissionCommandValidator : AbstractValidator<CreatePermissionCommand>
{
    public CreatePermissionCommandValidator()
    {
        RuleFor(x => x.ResourceCode).NotEmpty();
        RuleFor(x => x.ResourceName).NotEmpty();
        RuleFor(x => x.ActionCode).NotEmpty();
        RuleFor(x => x.ActionName).NotEmpty();
    }
}
