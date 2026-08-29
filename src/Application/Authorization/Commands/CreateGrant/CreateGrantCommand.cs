using qc_authorization.Application.Authorization.Audit;
using qc_authorization.Application.Common.Interfaces;
using qc_authorization.Domain.Authorization;
using qc_authorization.Domain.Authorization.Enums;
using qc_authorization.Domain.Authorization.ValueObjects;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace qc_authorization.Application.Authorization.Commands.CreateGrant;

public record CreateGrantCommand(
    SubjectType SubjectType,
    Guid SubjectId,
    Guid? SubjectUserId,
    Guid PermissionId,
    string? Resource,
    string? ResourceId,
    ScopeKind ScopeKind,
    string? ScopeIdentifier,
    Effect Effect,
    SourceType SourceType,
    Guid SourceId,
    DateTimeOffset ValidFrom,
    DateTimeOffset? ValidTo,
    int Priority) : IRequest<Guid>;

public class CreateGrantCommandHandler : IRequestHandler<CreateGrantCommand, Guid>
{
    private readonly IApplicationDbContext _context;
    private readonly IAuthorizationAuditService _audit;

    public CreateGrantCommandHandler(IApplicationDbContext context, IAuthorizationAuditService audit)
    {
        _context = context;
        _audit = audit;
    }

    public async Task<Guid> Handle(CreateGrantCommand request, CancellationToken cancellationToken)
    {
        _ = await _context.Permissions
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == request.PermissionId, cancellationToken)
            ?? throw new InvalidOperationException($"Permission {request.PermissionId} not found.");

        Grant grant = request.SubjectType == SubjectType.User && request.SubjectUserId is Guid subjectUserId
            ? Grant.CreateForUser(
                subjectUserId,
                request.PermissionId,
                request.SourceType,
                request.SourceId,
                request.Effect,
                request.ValidFrom,
                request.ValidTo,
                request.Priority,
                resource: request.Resource,
                resourceId: request.ResourceId,
                scopeKind: request.ScopeKind,
                scopeIdentifier: request.ScopeIdentifier)
            : Grant.Create(
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
                request.ScopeIdentifier,
                subjectUserId: request.SubjectUserId);

        _context.Grants.Add(grant);
        await _audit.RecordAsync(
            "GrantCreated",
            null,
            $"grantId=pending;subject={grant.SubjectType}:{grant.SubjectId};permissionId={grant.PermissionId}",
            cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return grant.Id;
    }
}
