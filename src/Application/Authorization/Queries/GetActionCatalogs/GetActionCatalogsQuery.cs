using qc_authorization.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace qc_authorization.Application.Authorization.Queries.GetActionCatalogs;

public record GetActionCatalogsQuery(string? SearchTerm = null) : IRequest<IReadOnlyList<ActionCatalogDto>>;

public record ActionCatalogDto(int Id, string Code, string Name);

public class GetActionCatalogsQueryHandler : IRequestHandler<GetActionCatalogsQuery, IReadOnlyList<ActionCatalogDto>>
{
    private readonly IApplicationDbContext _context;

    public GetActionCatalogsQueryHandler(IApplicationDbContext context) => _context = context;

    public async Task<IReadOnlyList<ActionCatalogDto>> Handle(GetActionCatalogsQuery request, CancellationToken cancellationToken)
    {
        var query = _context.ActionCatalogs.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var term = request.SearchTerm.Trim().ToLower();
            query = query.Where(a => a.Code.ToLower().Contains(term) || a.Name.ToLower().Contains(term));
        }

        return await query
            .OrderBy(a => a.Code)
            .Select(a => new ActionCatalogDto(a.Id, a.Code, a.Name))
            .ToListAsync(cancellationToken);
    }
}
