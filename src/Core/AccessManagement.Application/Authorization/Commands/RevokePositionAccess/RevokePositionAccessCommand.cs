using AccessManagement.Application.Abstractions;
using AccessManagement.Application.Authorization.Audit;
using AccessManagement.Application.Common.Interfaces;
using AccessManagement.Application.Common.Security;
using AccessManagement.Domain.Authorization;
using AccessManagement.Domain.Authorization.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AccessManagement.Application.Authorization.Commands.RevokePositionAccess;

public sealed record RevokePositionAccessCommand(
    Guid PositionId,
    Guid PermissionId,
    Guid? ScopeUnitId,
    Guid RevokedBy) : IRequest, IRequireUserAdmin;

public sealed class RevokePositionAccessHandler : IRequestHandler<RevokePositionAccessCommand>
{
    private readonly IApplicationDbContext _db;
    private readonly IPositionHierarchyQuery _hierarchy;
    private readonly IAuthorizationAuditService _audit;

    public RevokePositionAccessHandler(
        IApplicationDbContext db,
        IPositionHierarchyQuery hierarchy,
        IAuthorizationAuditService audit)
    {
        _db = db;
        _hierarchy = hierarchy;
        _audit = audit;
    }

    public async Task Handle(RevokePositionAccessCommand cmd, CancellationToken ct)
    {
        var descendantIds = await _hierarchy.GetDescendantsAsync(cmd.PositionId, ct);
        var affected = descendantIds.Append(cmd.PositionId).Distinct().ToList();
        var now = DateTimeOffset.UtcNow;

        var grants = await _db.Grants
            .Where(g => g.SubjectType == SubjectType.Position
                     && affected.Contains(g.SubjectId)
                     && g.PermissionId == cmd.PermissionId
                     && g.ScopeUnitId == cmd.ScopeUnitId
                     && (g.ValidTo == null || g.ValidTo > now))
            .ToListAsync(ct);

        foreach (var grant in grants)
        {
            grant.Deactivate(now);
        }

        await _audit.RecordAsync(
            "PositionAccessRevoked",
            cmd.RevokedBy,
            $"affected={affected.Count};permissionId={cmd.PermissionId}",
            ct);

        await _db.SaveChangesAsync(ct);
    }
}
