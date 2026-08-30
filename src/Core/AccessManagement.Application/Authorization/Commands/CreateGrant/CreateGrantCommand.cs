using AccessManagement.Application.Authorization.Audit;
using AccessManagement.Application.Common.Interfaces;
using AccessManagement.Domain.Authorization;
using AccessManagement.Domain.Authorization.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AccessManagement.Application.Authorization.Commands.CreateGrant;

public record CreateGrantCommand(
    SubjectType SubjectType,
    Guid SubjectId,
    Guid? SubjectUserId,
    Guid PermissionId,
    string? Resource,
    string? ResourceId,
    Guid? ScopeUnitId,
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
                scopeUnitId: request.ScopeUnitId)
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
                request.ScopeUnitId,
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
