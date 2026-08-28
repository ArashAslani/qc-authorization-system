namespace qc_authorization.Application.Authorization.Commands.RevokeGrant;

public class RevokeGrantCommandValidator : AbstractValidator<RevokeGrantCommand>
{
    public RevokeGrantCommandValidator()
    {
        RuleFor(x => x.GrantId).GreaterThan(0);

        When(x => x.ActorUserId.HasValue, () =>
        {
            RuleFor(x => x.ActorUserId!.Value).GreaterThan(0);
        });
    }
}
