using qc_authorization.Application.Common.Interfaces;
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
    private readonly IApplicationDbContext _context;

    public CreateGrantCommandHandler(IApplicationDbContext context) => _context = context;

    public async Task<int> Handle(CreateGrantCommand request, CancellationToken cancellationToken)
    {
        // Scope validation: bounded scopes need an identifier.
        if (request.ScopeKind != ScopeKind.Unbounded && string.IsNullOrWhiteSpace(request.ScopeIdentifier))
        {
            throw new ArgumentException(
                $"Scope kind '{request.ScopeKind}' requires a non-empty identifier.",
                nameof(request.ScopeIdentifier));
        }

        // Validity: end cannot precede start.
        if (request.ValidTo is { } end && end < request.ValidFrom)
        {
            throw new ArgumentException(
                "ValidTo cannot be earlier than ValidFrom.", nameof(request.ValidTo));
        }

        // Source-traceability invariant: a Grant must always be traceable
        // back to the entity that created it. (SourceType, SourceId) is
        // never null on a Grant.
        var grant = new Grant
        {
            SubjectType = request.SubjectType,
            SubjectId = request.SubjectId,
            PermissionId = request.PermissionId,
            Resource = request.Resource,
            ResourceId = request.ResourceId,
            ScopeKind = request.ScopeKind,
            ScopeIdentifier = request.ScopeIdentifier,
            Effect = request.Effect,
            SourceType = request.SourceType,
            SourceId = request.SourceId,
            ValidFrom = request.ValidFrom,
            ValidTo = request.ValidTo,
            Priority = request.Priority,
        };

        _context.Grants.Add(grant);
        await _context.SaveChangesAsync(cancellationToken);
        return grant.Id;
    }
}
