namespace qc_authorization.Application.Authorization.Commands.RevokeGrant;

public class RevokeGrantCommandValidator : AbstractValidator<RevokeGrantCommand>
{
    public RevokeGrantCommandValidator()
    {
        RuleFor(x => x.GrantId).NotEqual(Guid.Empty);

        When(x => x.ActorUserId.HasValue, () =>
        {
            RuleFor(x => x.ActorUserId!.Value).NotEqual(Guid.Empty);
        });
    }
}
