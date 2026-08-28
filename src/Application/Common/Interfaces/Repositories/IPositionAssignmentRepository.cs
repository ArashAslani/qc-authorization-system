namespace qc_authorization.Application.Common.Interfaces.Repositories;

public interface IPositionAssignmentRepository
{
    Task<IReadOnlyList<int>> GetActivePositionIdsForPersonnelAsync(
        int personnelId,
        DateTimeOffset when,
        CancellationToken cancellationToken = default);
}
