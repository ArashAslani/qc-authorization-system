using AccessManagement.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AccessManagement.Application.Authorization.Queries.GetResourceCatalogs;

public record GetResourceCatalogsQuery(string? SearchTerm = null) : IRequest<IReadOnlyList<ResourceCatalogDto>>;

public record ResourceCatalogDto(Guid Id, string Code, string Name, string? Description);

public class GetResourceCatalogsQueryHandler : IRequestHandler<GetResourceCatalogsQuery, IReadOnlyList<ResourceCatalogDto>>
{
    private readonly IApplicationDbContext _context;

    public GetResourceCatalogsQueryHandler(IApplicationDbContext context) => _context = context;

    public async Task<IReadOnlyList<ResourceCatalogDto>> Handle(GetResourceCatalogsQuery request, CancellationToken cancellationToken)
    {
        var query = _context.ResourceCatalogs.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var term = request.SearchTerm.Trim().ToLower();
            query = query.Where(r => r.Code.ToLower().Contains(term) || r.Name.ToLower().Contains(term));
        }

        return await query
            .OrderBy(r => r.Code)
            .Select(r => new ResourceCatalogDto(r.Id, r.Code, r.Name, r.Description))
            .ToListAsync(cancellationToken);
    }
}
