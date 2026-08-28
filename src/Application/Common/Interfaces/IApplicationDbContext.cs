using qc_authorization.Domain.Organization;

namespace qc_authorization.Application.Common.Interfaces;

public interface IApplicationDbContext
{
    DbSet<Personnel> Personnel { get; }
    DbSet<Position> Positions { get; }
    DbSet<PositionAssignment> PositionAssignments { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
