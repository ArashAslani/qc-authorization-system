using AccessManagement.Application.Abstractions;
using AccessManagement.Application.Common.Interfaces;
using AccessManagement.Domain.Organization;
using Microsoft.EntityFrameworkCore;

namespace AccessManagement.Application.Organization;

public sealed class PositionHierarchyQuery : IPositionHierarchyQuery
{
    private readonly IApplicationDbContext _db;
    private readonly PositionHierarchyService _hierarchy;

    public PositionHierarchyQuery(IApplicationDbContext db, PositionHierarchyService hierarchy)
    {
        _db = db;
        _hierarchy = hierarchy;
    }

    public async Task<IReadOnlyList<Guid>> GetAncestorsAsync(Guid positionId, CancellationToken ct = default)
    {
        var positions = await _db.Positions.AsNoTracking().ToListAsync(ct);
        var position = positions.FirstOrDefault(p => p.Id == positionId);
        if (position is null)
        {
            return Array.Empty<Guid>();
        }

        return _hierarchy.Ancestors(position, positions).Select(p => p.Id).ToList();
    }

    public async Task<IReadOnlyList<Guid>> GetDescendantsAsync(Guid positionId, CancellationToken ct = default)
    {
        var positions = await _db.Positions.AsNoTracking().ToListAsync(ct);
        var position = positions.FirstOrDefault(p => p.Id == positionId);
        if (position is null)
        {
            return Array.Empty<Guid>();
        }

        return _hierarchy.Descendants(position, positions).Select(p => p.Id).ToList();
    }
}
