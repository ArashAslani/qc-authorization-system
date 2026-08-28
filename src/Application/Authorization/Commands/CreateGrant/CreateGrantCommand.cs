using qc_authorization.Application.Common.Interfaces;
using qc_authorization.Application.Common.Interfaces.Repositories;
using qc_authorization.Domain.Authorization;
using qc_authorization.Domain.Authorization.Enums;
using qc_authorization.Domain.Authorization.ValueObjects;
using MediatR;

namespace qc_authorization.Application.Authorization.Commands.CreateGrant;

public record CreateGrantCommand(
    SubjectType SubjectType,
    int SubjectId,
    int PermissionId,
    string? Resource,
    string? ResourceId,
    ScopeKind ScopeKind,
    string? ScopeIdentifier,
    Effect Effect,
    SourceType SourceType,
    int SourceId,
    DateTimeOffset ValidFrom,
    DateTimeOffset? ValidTo,
    int Priority) : IRequest<int>;

public class CreateGrantCommandHandler : IRequestHandler<CreateGrantCommand, int>
{
    private readonly IGrantRepository _grants;
    private readonly IUnitOfWork _unitOfWork;

    public CreateGrantCommandHandler(IGrantRepository grants, IUnitOfWork unitOfWork)
    {
        _grants = grants;
        _unitOfWork = unitOfWork;
    }

    public async Task<int> Handle(CreateGrantCommand request, CancellationToken cancellationToken)
    {
        var grant = Grant.Create(
            request.SubjectType,
            request.SubjectId,
            request.PermissionId,
            request.SourceType,
            request.SourceId,
            request.Effect,
            request.ValidFrom,
            request.ValidTo,
            request.Priority,
            request.Resource,
            request.ResourceId,
            request.ScopeKind,
            request.ScopeIdentifier);

        await _grants.AddAsync(grant, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return grant.Id;
    }
}
