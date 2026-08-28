using qc_authorization.Domain.Organization;

namespace qc_authorization.Application.Common.Interfaces.Repositories;

public interface IPositionAssignmentRepository
{
    Task AddAsync(PositionAssignment assignment, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<int>> GetActivePositionIdsForPersonnelAsync(
        int personnelId,
        DateTimeOffset when,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<int>> GetActivePositionIdsForSystemUserAsync(
        int systemUserId,
        DateTimeOffset when,
        CancellationToken cancellationToken = default);
}
