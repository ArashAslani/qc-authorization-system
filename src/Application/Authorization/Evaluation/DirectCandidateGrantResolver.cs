using qc_authorization.Application.Common.Interfaces;
using qc_authorization.Domain.Authorization;
using Microsoft.EntityFrameworkCore;

namespace qc_authorization.Application.Authorization.Evaluation;

/// <summary>
/// Phase 03 resolver: returns grants whose subject matches the request
/// directly. No propagation. The same set of grants is used by the
/// evaluator regardless of which source (Role, Position, User,
/// RoleGroup) created them.
/// </summary>
public class DirectCandidateGrantResolver : ICandidateGrantResolver
{
    private readonly IApplicationDbContext _context;

    public DirectCandidateGrantResolver(IApplicationDbContext context) => _context = context;

    public async Task<IReadOnlyList<Grant>> ResolveAsync(AccessRequest request, CancellationToken cancellationToken)
    {
        var normalized = request.NormalizedPermissionCode;

        var permission = await _context.Permissions
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Code.ToUpper() == normalized, cancellationToken);

        if (permission is null)
        {
            return Array.Empty<Grant>();
        }

        return await _context.Grants
            .AsNoTracking()
            .Where(g => g.SubjectType == request.SubjectType
                     && g.SubjectId == request.SubjectId
                     && g.PermissionId == permission.Id
                     && (g.Resource == null || g.Resource == request.Resource))
            .ToListAsync(cancellationToken);
    }
}
