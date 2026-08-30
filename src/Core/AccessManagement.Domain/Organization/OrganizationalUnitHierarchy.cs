using AccessManagement.Domain.Organization.Exceptions;

namespace AccessManagement.Domain.Organization;

public static class OrganizationalUnitHierarchy
{
    public static IReadOnlyList<OrganizationalUnit> Ancestors(
        OrganizationalUnit unit,
        IReadOnlyCollection<OrganizationalUnit> allUnits)
    {
        ArgumentNullException.ThrowIfNull(unit);
        ArgumentNullException.ThrowIfNull(allUnits);

        var byId = allUnits.ToDictionary(u => u.Id);
        var result = new List<OrganizationalUnit>();
        var current = unit.ParentId is Guid parentId ? byId.GetValueOrDefault(parentId) : null;

        for (var i = 0; i < allUnits.Count + 1 && current is not null; i++)
        {
            result.Add(current);
            current = current.ParentId is Guid pid ? byId.GetValueOrDefault(pid) : null;
        }

        if (current is not null)
        {
            throw new HierarchyCycleException(
                $"Cycle detected while walking ancestors of unit {unit.Id}.");
        }

        return result;
    }

    public static IReadOnlyList<OrganizationalUnit> Descendants(
        OrganizationalUnit unit,
        IReadOnlyCollection<OrganizationalUnit> allUnits)
    {
        ArgumentNullException.ThrowIfNull(unit);
        ArgumentNullException.ThrowIfNull(allUnits);

        var result = new List<OrganizationalUnit>();
        var children = allUnits.Where(u => u.ParentId == unit.Id).ToList();
        var queue = new Queue<OrganizationalUnit>(children);
        var visited = new HashSet<Guid> { unit.Id };

        while (queue.Count > 0)
        {
            var node = queue.Dequeue();
            if (!visited.Add(node.Id))
            {
                throw new HierarchyCycleException(
                    $"Cycle detected while walking descendants of unit {unit.Id}.");
            }

            result.Add(node);
            foreach (var child in allUnits.Where(u => u.ParentId == node.Id))
            {
                queue.Enqueue(child);
            }
        }

        return result;
    }

    public static bool IsDescendantOf(
        Guid unitId,
        Guid ancestorId,
        IReadOnlyCollection<OrganizationalUnit> allUnits)
    {
        if (unitId == ancestorId)
        {
            return true;
        }

        var unit = allUnits.FirstOrDefault(u => u.Id == unitId);
        if (unit is null)
        {
            return false;
        }

        return Ancestors(unit, allUnits).Any(a => a.Id == ancestorId);
    }

    public static void EnsureValidParenting(
        OrganizationalUnit unit,
        OrganizationalUnit? newParent,
        IReadOnlyCollection<OrganizationalUnit> allUnits)
    {
        ArgumentNullException.ThrowIfNull(unit);
        ArgumentNullException.ThrowIfNull(allUnits);

        if (newParent is null)
        {
            return;
        }

        if (newParent.Id == unit.Id)
        {
            throw new HierarchyCycleException($"Unit {unit.Id} cannot be its own parent.");
        }

        if (Ancestors(newParent, allUnits).Any(a => a.Id == unit.Id))
        {
            throw new HierarchyCycleException(
                $"Re-parenting {unit.Id} under {newParent.Id} would create a cycle.");
        }
    }
}
