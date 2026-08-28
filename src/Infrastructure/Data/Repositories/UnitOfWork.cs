using qc_authorization.Application.Common.Interfaces;
using qc_authorization.Infrastructure.Data;

namespace qc_authorization.Infrastructure.Data.Repositories;

public sealed class UnitOfWork(ApplicationDbContext context) : IUnitOfWork
{
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        context.SaveChangesAsync(cancellationToken);
}
