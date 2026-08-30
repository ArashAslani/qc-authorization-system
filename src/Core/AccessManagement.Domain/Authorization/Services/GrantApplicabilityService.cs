using AccessManagement.Domain.Authorization.Enums;
using AccessManagement.Domain.Organization;

namespace AccessManagement.Domain.Authorization.Services;

/// <summary>
/// Pure domain rules for deciding whether a stored grant applies to an
/// access request. Propagation direction is asymmetric and explicit.
/// </summary>
public sealed class GrantApplicabilityService
{
    private readonly PositionHierarchyService _hierarchy;

    public GrantApplicabilityService(PositionHierarchyService hierarchy) =>
        _hierarchy = hierarchy;

    public static bool IsIndividualGrant(Grant grant) =>
        grant.SubjectType == SubjectType.User && grant.SourceType == SourceType.User;

    public bool Applies(
        Grant grant,
        SubjectType requestSubjectType,
        Guid requestSubjectId,
        Guid? requestUserId,
        IReadOnlySet<Guid> requestPositionIds,
        IReadOnlyCollection<Position> allPositions)
    {
        // Every user-subject grant (individual, role-materialized, delegated)
        // applies only to that user and never follows the position tree.
        if (grant.SubjectType == SubjectType.User)
        {
            return requestUserId == grant.SubjectUserId;
        }

        if (grant.SubjectType == SubjectType.Role || grant.SubjectType == SubjectType.RoleGroup)
        {
            return requestSubjectType == grant.SubjectType
                && requestSubjectId == grant.SubjectId;
        }

        if (grant.SubjectType == SubjectType.Position)
        {
            if (requestSubjectType != SubjectType.User
                && requestSubjectType != SubjectType.Position)
            {
                return false;
            }

            var grantPosition = allPositions.FirstOrDefault(p => p.Id == grant.SubjectId);
            if (grantPosition is null)
            {
                return false;
            }

            var effectiveIds = EffectivePositionIds(grant, grantPosition, allPositions);
            return requestPositionIds.Any(effectiveIds.Contains);
        }

        return requestSubjectType == grant.SubjectType
            && requestSubjectId == grant.SubjectId;
    }

    public HashSet<Guid> EffectivePositionIds(
        Grant grant,
        Position grantPosition,
        IReadOnlyCollection<Position> allPositions)
    {
        var ids = new HashSet<Guid> { grantPosition.Id };

        if (grant.Effect == Effect.Allow)
        {
            foreach (var ancestor in _hierarchy.Ancestors(grantPosition, allPositions))
            {
                ids.Add(ancestor.Id);
            }
        }

        return ids;
    }
}
