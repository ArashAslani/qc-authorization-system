using AccessManagement.Application.Abstractions;

namespace AccessManagement.Application.Authorization.Evaluation;

public sealed class ScopeMatcher : IScopeMatcher
{
    private readonly IOrganizationalUnitHierarchy _units;

    public ScopeMatcher(IOrganizationalUnitHierarchy units) => _units = units;

    public async Task<bool> MatchesAsync(Guid? grantScopeUnitId, Guid? resourceScopeUnitId, CancellationToken ct)
    {
        if (grantScopeUnitId is null)
        {
            return true;
        }

        if (resourceScopeUnitId is null)
        {
            return false;
        }

        if (grantScopeUnitId == resourceScopeUnitId)
        {
            return true;
        }

        return await _units.IsDescendantOfAsync(resourceScopeUnitId.Value, grantScopeUnitId.Value, ct);
    }
}
