using qc_authorization.Domain.Organization.Exceptions;

namespace qc_authorization.Domain.Organization;

/// <summary>
/// Pure in-memory operations over a known set of <see cref="Position"/> nodes.
/// </summary>
public class PositionHierarchyService
{
    public IReadOnlyList<Position> Children(Position position, IReadOnlyCollection<Position> allPositions)
    {
        ArgumentNullException.ThrowIfNull(position);
        ArgumentNullException.ThrowIfNull(allPositions);
        return allPositions.Where(p => p.ParentPositionId == position.Id).ToList();
    }

    public IReadOnlyList<Position> Ancestors(Position position, IReadOnlyCollection<Position> allPositions)
    {
        ArgumentNullException.ThrowIfNull(position);
        ArgumentNullException.ThrowIfNull(allPositions);

        var byId = allPositions.ToDictionary(p => p.Id);
        var result = new List<Position>();
        var current = position.ParentPositionId is Guid parentId ? byId.GetValueOrDefault(parentId) : null;

        for (var i = 0; i < allPositions.Count + 1 && current is not null; i++)
        {
            result.Add(current);
            current = current.ParentPositionId is Guid pid ? byId.GetValueOrDefault(pid) : null;
        }

        if (current is not null)
        {
            throw new HierarchyCycleException(
                $"Cycle detected while walking ancestors of position {position.Id}.");
        }

        return result;
    }

    public IReadOnlyList<Position> Descendants(Position position, IReadOnlyCollection<Position> allPositions)
    {
        ArgumentNullException.ThrowIfNull(position);
        ArgumentNullException.ThrowIfNull(allPositions);

        var result = new List<Position>();
        var queue = new Queue<Position>(Children(position, allPositions));
        var visited = new HashSet<Guid> { position.Id };

        while (queue.Count > 0)
        {
            var node = queue.Dequeue();
            if (!visited.Add(node.Id))
            {
                throw new HierarchyCycleException(
                    $"Cycle detected while walking descendants of position {position.Id}.");
            }
            result.Add(node);
            foreach (var child in Children(node, allPositions))
            {
                queue.Enqueue(child);
            }
        }

        return result;
    }

    public void EnsureValidParenting(
        Position position,
        Position? newParent,
        IReadOnlyCollection<Position> allPositions)
    {
        ArgumentNullException.ThrowIfNull(position);
        ArgumentNullException.ThrowIfNull(allPositions);

        if (newParent is null)
        {
            return;
        }

        if (newParent.CompanyId != position.CompanyId)
        {
            throw new OrganizationDomainException(
                $"Position {position.Id} (company {position.CompanyId}) cannot have parent {newParent.Id} (company {newParent.CompanyId}).");
        }

        if (newParent.Id == position.Id)
        {
            throw new HierarchyCycleException(
                $"Position {position.Id} cannot be its own parent.");
        }

        if (Ancestors(newParent, allPositions).Any(a => a.Id == position.Id))
        {
            throw new HierarchyCycleException(
                $"Re-parenting {position.Id} under {newParent.Id} would create a cycle.");
        }
    }
}
