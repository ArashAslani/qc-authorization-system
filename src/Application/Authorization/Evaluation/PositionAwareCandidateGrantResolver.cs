using qc_authorization.Application.Common.Interfaces.Repositories;
using qc_authorization.Domain.Authorization;
using qc_authorization.Domain.Authorization.Evaluation;
using qc_authorization.Domain.Authorization.Enums;
using qc_authorization.Domain.Authorization.Services;
using qc_authorization.Domain.Organization;

namespace qc_authorization.Application.Authorization.Evaluation;

/// <summary>
/// Resolves candidate grants using repositories and domain propagation rules.
/// </summary>
public sealed class PositionAwareCandidateGrantResolver : ICandidateGrantResolver
{
    private readonly IPermissionRepository _permissions;
    private readonly IGrantRepository _grants;
    private readonly IPositionRepository _positions;
    private readonly IPositionAssignmentRepository _assignments;
    private readonly IDelegationRepository _delegations;
    private readonly GrantApplicabilityService _applicability;

    public PositionAwareCandidateGrantResolver(
        IPermissionRepository permissions,
        IGrantRepository grants,
        IPositionRepository positions,
        IPositionAssignmentRepository assignments,
        IDelegationRepository delegations,
        GrantApplicabilityService applicability)
    {
        _permissions = permissions;
        _grants = grants;
        _positions = positions;
        _assignments = assignments;
        _delegations = delegations;
        _applicability = applicability;
    }

    public async Task<IReadOnlyList<Grant>> ResolveAsync(AccessRequest request, CancellationToken cancellationToken)
    {
        var permission = await _permissions.GetByCodeAsync(request.NormalizedPermissionCode, cancellationToken);
        if (permission is null)
        {
            return Array.Empty<Grant>();
        }

        var allGrants = await _grants.GetByPermissionAndResourceAsync(
            permission.Id,
            request.Resource,
            cancellationToken);

        var allPositions = await _positions.GetAllAsync(cancellationToken);
        var requestPositionIds = await ResolveRequestPositions(request, cancellationToken);

        var result = new List<Grant>();

        foreach (var grant in allGrants)
        {
            if (_applicability.Applies(
                    grant,
                    request.SubjectType,
                    request.SubjectId,
                    requestPositionIds,
                    allPositions))
            {
                result.Add(grant);
            }
        }

        foreach (var delegation in await _delegations.GetActiveForDelegateAsync(
                     request.SubjectId,
                     request.When,
                     cancellationToken))
        {
            if (delegation.PermissionId != permission.Id)
            {
                continue;
            }

            if (request.SubjectType != SubjectType.User)
            {
                continue;
            }

            var delegatedGrant = delegation.ToGrant();
            if (_applicability.Applies(
                    delegatedGrant,
                    request.SubjectType,
                    request.SubjectId,
                    requestPositionIds,
                    allPositions))
            {
                result.Add(delegatedGrant);
            }
        }

        return result;
    }

    private async Task<HashSet<int>> ResolveRequestPositions(AccessRequest request, CancellationToken ct)
    {
        if (request.SubjectType == SubjectType.Position)
        {
            return [request.SubjectId];
        }

        if (request.SubjectType != SubjectType.User)
        {
            return [];
        }

        var assignments = await _assignments.GetActivePositionIdsForPersonnelAsync(
            request.SubjectId,
            request.When,
            ct);

        return new HashSet<int>(assignments);
    }
}
