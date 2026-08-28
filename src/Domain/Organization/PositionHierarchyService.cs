using qc_authorization.Domain.Organization.Exceptions;

namespace qc_authorization.Domain.Organization;

/// <summary>
/// Pure in-memory operations over a known set of <see cref="Position"/> nodes.
/// Persistence and concurrency are not its concern: it is deterministic,
/// side-effect free, and unit-testable.
/// </summary>
public class PositionHierarchyService
{
    public IReadOnlyList<Position> Children(Position position, IReadOnlyCollection<Position> allPositions)
    {
        ArgumentNullException.ThrowIfNull(position);
        ArgumentNullException.ThrowIfNull(allPositions);
        return allPositions.Where(p => p.ParentId == position.Id).ToList();
    }

    /// <summary>
    /// Returns the strict ancestors of <paramref name="position"/>, ordered
    /// from the position's direct parent up to the root.
    /// </summary>
    public IReadOnlyList<Position> Ancestors(Position position, IReadOnlyCollection<Position> allPositions)
    {
        ArgumentNullException.ThrowIfNull(position);
        ArgumentNullException.ThrowIfNull(allPositions);

        var byId = allPositions.ToDictionary(p => p.Id);
        var result = new List<Position>();
        var current = position.ParentId is int parentId ? byId.GetValueOrDefault(parentId) : null;

        // Hard cap to defend against pre-existing cycles in stored data.
        for (var i = 0; i < allPositions.Count + 1 && current is not null; i++)
        {
            result.Add(current);
            current = current.ParentId is int pid ? byId.GetValueOrDefault(pid) : null;
        }

        if (current is not null)
        {
            throw new HierarchyCycleException(
                $"Cycle detected while walking ancestors of position {position.Id}.");
        }

        return result;
    }

    /// <summary>
    /// Returns the strict descendants of <paramref name="position"/> in
    /// pre-order (parent before children, siblings in storage order).
    /// </summary>
    public IReadOnlyList<Position> Descendants(Position position, IReadOnlyCollection<Position> allPositions)
    {
        ArgumentNullException.ThrowIfNull(position);
        ArgumentNullException.ThrowIfNull(allPositions);

        var result = new List<Position>();
        var queue = new Queue<Position>(Children(position, allPositions));
        var visited = new HashSet<int> { position.Id };

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

    /// <summary>
    /// Throws if assigning <paramref name="newParent"/> as the parent of
    /// <paramref name="position"/> would create a self-reference, a direct
    /// cycle, or an indirect cycle.
    /// </summary>
    public void EnsureValidParenting(
        Position position,
        Position? newParent,
        IReadOnlyCollection<Position> allPositions)
    {
        ArgumentNullException.ThrowIfNull(position);
        ArgumentNullException.ThrowIfNull(allPositions);

        if (newParent is null)
        {
            return; // detaching is always allowed
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
