using qc_authorization.Application.Common.Interfaces.Repositories;
using qc_authorization.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace qc_authorization.Infrastructure.Data.Repositories;

public sealed class PositionAssignmentRepository(ApplicationDbContext context) : IPositionAssignmentRepository
{
    public async Task<IReadOnlyList<int>> GetActivePositionIdsForPersonnelAsync(
        int personnelId,
        DateTimeOffset when,
        CancellationToken cancellationToken = default) =>
        await context.PositionAssignments
            .AsNoTracking()
            .Where(a => a.PersonnelId == personnelId
                     && a.ValidFrom <= when
                     && (a.ValidTo == null || when <= a.ValidTo))
            .Select(a => a.PositionId)
            .ToListAsync(cancellationToken);
}
