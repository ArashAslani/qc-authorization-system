using AccessManagement.Application.Common.Interfaces;
using MediatR;

namespace AccessManagement.Application.Organization.Commands.LinkPersonnelToIdentityUser;

public record LinkPersonnelToIdentityUserCommand(Guid PersonnelId, Guid IdentityUserId) : IRequest;

public class LinkPersonnelToIdentityUserCommandHandler : IRequestHandler<LinkPersonnelToIdentityUserCommand>
{
    private readonly IPersonnelIdentityBridge _bridge;

    public LinkPersonnelToIdentityUserCommandHandler(IPersonnelIdentityBridge bridge) =>
        _bridge = bridge;

    public Task Handle(LinkPersonnelToIdentityUserCommand request, CancellationToken cancellationToken) =>
        _bridge.LinkAsync(request.PersonnelId, request.IdentityUserId, cancellationToken);
}
