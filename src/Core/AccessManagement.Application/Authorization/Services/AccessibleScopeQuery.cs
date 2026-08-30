using AccessManagement.Application.Abstractions;
using AccessManagement.Domain.Authorization;

namespace AccessManagement.Application.Authorization.Services;

/// <summary>
/// Applies A1.2 accessible roots minus denied subtrees to an in-memory scope id
/// or to a grant query (allowed subtree minus denied subtree).
/// </summary>
public static class AccessibleScopeQuery
{
    public static IQueryable<Grant> ApplyAccessibleScopes(
        IQueryable<Grant> query,
        IReadOnlyCollection<Guid> allowedUnitIds,
        IReadOnlyCollection<Guid> deniedUnitIds,
        bool isUnrestricted)
    {
        var allowed = allowedUnitIds.ToList();
        var denied = deniedUnitIds.ToList();

        if (isUnrestricted)
        {
            return denied.Count == 0
                ? query
                : query.Where(g => g.ScopeUnitId == null || !denied.Contains(g.ScopeUnitId.Value));
        }

        return query.Where(g =>
            g.ScopeUnitId == null
            || (allowed.Contains(g.ScopeUnitId.Value) && !denied.Contains(g.ScopeUnitId.Value)));
    }

    public static bool Includes(
        Guid? scopeUnitId,
        AccessibleScopeResult scopes,
        IReadOnlySet<Guid> allowedUnitIds,
        IReadOnlySet<Guid> deniedUnitIds)
    {
        if (scopes.IsUnrestricted)
        {
            return scopeUnitId is not Guid id || !deniedUnitIds.Contains(id);
        }

        if (scopeUnitId is not Guid unitId)
        {
            return false;
        }

        return allowedUnitIds.Contains(unitId) && !deniedUnitIds.Contains(unitId);
    }

    public static async Task<(HashSet<Guid> Allowed, HashSet<Guid> Denied)> ExpandAsync(
        AccessibleScopeResult scopes,
        IOrganizationalUnitHierarchy units,
        CancellationToken ct = default)
    {
        var allowed = new HashSet<Guid>();
        var denied = new HashSet<Guid>();

        foreach (var root in scopes.ScopeRootUnitIds)
        {
            allowed.Add(root);
            foreach (var child in await units.GetDescendantIdsAsync(root, ct))
            {
                allowed.Add(child);
            }
        }

        foreach (var hole in scopes.DeniedScopeUnitIds)
        {
            denied.Add(hole);
            foreach (var child in await units.GetDescendantIdsAsync(hole, ct))
            {
                denied.Add(child);
            }
        }

        allowed.ExceptWith(denied);
        return (allowed, denied);
    }
}
