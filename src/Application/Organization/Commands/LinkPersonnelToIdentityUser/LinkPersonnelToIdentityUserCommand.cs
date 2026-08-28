using qc_authorization.Application.Common.Interfaces;
using MediatR;

namespace qc_authorization.Application.Organization.Commands.LinkPersonnelToIdentityUser;

public record LinkPersonnelToIdentityUserCommand(int PersonnelId, Guid IdentityUserId) : IRequest;

public class LinkPersonnelToIdentityUserCommandHandler : IRequestHandler<LinkPersonnelToIdentityUserCommand>
{
    private readonly IPersonnelIdentityBridge _bridge;

    public LinkPersonnelToIdentityUserCommandHandler(IPersonnelIdentityBridge bridge) =>
        _bridge = bridge;

    public Task Handle(LinkPersonnelToIdentityUserCommand request, CancellationToken cancellationToken) =>
        _bridge.LinkAsync(request.PersonnelId, request.IdentityUserId, cancellationToken);
}
