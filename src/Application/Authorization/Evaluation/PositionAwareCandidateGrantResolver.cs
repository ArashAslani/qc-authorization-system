using qc_authorization.Application.Common.Interfaces;
using qc_authorization.Domain.Authorization;
using qc_authorization.Domain.Authorization.Enums;
using qc_authorization.Domain.Organization;
using Microsoft.EntityFrameworkCore;

namespace qc_authorization.Application.Authorization.Evaluation;

/// <summary>
/// Phase 04 candidate resolver.
///
/// Implements asymmetric position propagation:
/// <list type="bullet">
///   <item>Position Allow grants propagate from P to Ancestors(P).</item>
///   <item>Position Deny grants propagate from P to Descendants(P).</item>
///   <item>Individual grants (SubjectType = User AND SourceType = User) are
///     completely isolated from position propagation.</item>
/// </list>
/// The hierarchy is read live, not materialized. There is no generic
/// Propagate(Position, Operation) helper; the two directions are computed
/// by the two dedicated methods in <see cref="PositionHierarchyService"/>.
/// </summary>
public class PositionAwareCandidateGrantResolver : ICandidateGrantResolver
{
    private readonly IApplicationDbContext _context;
    private readonly PositionHierarchyService _hierarchy;

    public PositionAwareCandidateGrantResolver(
        IApplicationDbContext context,
        PositionHierarchyService hierarchy)
    {
        _context = context;
        _hierarchy = hierarchy;
    }

    public async Task<IReadOnlyList<Grant>> ResolveAsync(AccessRequest request, CancellationToken cancellationToken)
    {
        var normalized = request.NormalizedPermissionCode;

        var permission = await _context.Permissions
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Code.ToUpper() == normalized, cancellationToken);

        if (permission is null)
        {
            return Array.Empty<Grant>();
        }

        var allGrants = await _context.Grants
            .AsNoTracking()
            .Where(g => g.PermissionId == permission.Id
                     && (g.Resource == null || g.Resource == request.Resource))
            .ToListAsync(cancellationToken);

        var result = new List<Grant>();

        foreach (var g in allGrants)
        {
            // Individual grant: applied only when the request subject is
            // the same user. No propagation in either direction.
            if (g.SubjectType == SubjectType.User && g.SourceType == SourceType.User)
            {
                if (request.SubjectType == SubjectType.User
                    && request.SubjectId == g.SubjectId
                    && g.SourceId == g.SubjectId)
                {
                    result.Add(g);
                }
                continue;
            }

            // Role / RoleGroup grants: bound to the request subject only,
            // no position involvement.
            if (g.SubjectType == SubjectType.Role || g.SubjectType == SubjectType.RoleGroup)
            {
                if (request.SubjectType == g.SubjectType && request.SubjectId == g.SubjectId)
                {
                    result.Add(g);
                }
                continue;
            }

            // Position grant: asymmetric propagation.
            if (g.SubjectType == SubjectType.Position)
            {
                if (request.SubjectType != SubjectType.User
                    && request.SubjectType != SubjectType.Position)
                {
                    continue;
                }

                // The user (or position) must be inside the grant's
                // effective scope, computed live from the hierarchy.
                var allPositions = await LoadAllPositions(cancellationToken);
                var subject = allPositions.FirstOrDefault(p => p.Id == g.SubjectId);
                if (subject is null)
                {
                    continue;
                }

                var effectiveIds = g.Effect == Effect.Allow
                    ? AncestorIdsIncludingSelf(subject, allPositions)
                    : DescendantIdsIncludingSelf(subject, allPositions);

                var requestPositions = await ResolveRequestPositions(request, cancellationToken);
                if (requestPositions.Any(p => effectiveIds.Contains(p)))
                {
                    result.Add(g);
                }
            }
        }

        return result;
    }

    private async Task<HashSet<int>> ResolveRequestPositions(AccessRequest request, CancellationToken ct)
    {
        if (request.SubjectType == SubjectType.Position)
        {
            return new HashSet<int> { request.SubjectId };
        }

        // SubjectType == User: pull the user's current position(s).
        // PositionAssignment uses a validity window. We pick assignments
        // active at request.When.
        var assignments = await _context.PositionAssignments
            .AsNoTracking()
            .Where(a => a.PersonnelId == request.SubjectId
                     && a.ValidFrom <= request.When
                     && (a.ValidTo == null || request.When <= a.ValidTo))
            .Select(a => a.PositionId)
            .ToListAsync(ct);

        return new HashSet<int>(assignments);
    }

    private async Task<List<Position>> LoadAllPositions(CancellationToken ct) =>
        await _context.Positions.AsNoTracking().ToListAsync(ct);

    /// <summary>
    /// Returns the ids of <paramref name="position"/> and its strict ancestors.
    /// Grant direction (Allow): P + Ancestors(P).
    /// </summary>
    private HashSet<int> AncestorIdsIncludingSelf(Position position, IReadOnlyCollection<Position> allPositions)
    {
        var ids = new HashSet<int> { position.Id };
        foreach (var a in _hierarchy.Ancestors(position, allPositions))
        {
            ids.Add(a.Id);
        }
        return ids;
    }

    /// <summary>
    /// Returns the ids of <paramref name="position"/> and its strict descendants.
    /// Revoke direction (Deny): P + Descendants(P).
    /// </summary>
    private HashSet<int> DescendantIdsIncludingSelf(Position position, IReadOnlyCollection<Position> allPositions)
    {
        var ids = new HashSet<int> { position.Id };
        foreach (var d in _hierarchy.Descendants(position, allPositions))
        {
            ids.Add(d.Id);
        }
        return ids;
    }
}
