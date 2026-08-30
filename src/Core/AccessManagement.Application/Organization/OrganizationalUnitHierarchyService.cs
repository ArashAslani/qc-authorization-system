using AccessManagement.Application.Abstractions;
using AccessManagement.Application.Common.Interfaces;
using AccessManagement.Domain.Organization;
using Microsoft.EntityFrameworkCore;

namespace AccessManagement.Application.Organization;

public sealed class OrganizationalUnitHierarchyService : IOrganizationalUnitHierarchy
{
    private readonly IApplicationDbContext _db;

    public OrganizationalUnitHierarchyService(IApplicationDbContext db) => _db = db;

    public async Task<bool> IsDescendantOfAsync(Guid unitId, Guid ancestorId, CancellationToken ct = default)
    {
        if (unitId == ancestorId)
        {
            return true;
        }

        var units = await _db.OrganizationalUnits.AsNoTracking().ToListAsync(ct);
        return OrganizationalUnitHierarchy.IsDescendantOf(unitId, ancestorId, units);
    }

    public async Task<IReadOnlyList<Guid>> GetDescendantIdsAsync(Guid unitId, CancellationToken ct = default)
    {
        var units = await _db.OrganizationalUnits.AsNoTracking().ToListAsync(ct);
        var unit = units.FirstOrDefault(u => u.Id == unitId);
        if (unit is null)
        {
            return Array.Empty<Guid>();
        }

        return OrganizationalUnitHierarchy.Descendants(unit, units).Select(u => u.Id).ToList();
    }

    public async Task<string?> GetUnitTypeAsync(Guid unitId, CancellationToken ct = default)
    {
        var unit = await _db.OrganizationalUnits.AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == unitId, ct);
        return unit?.UnitType;
    }
}
