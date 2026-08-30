using AccessManagement.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AccessManagement.Application.Authorization.Audit.Queries.GetAuditEntries;

public record GetAuthorizationAuditEntriesQuery(
    string? EventType = null,
    Guid? ActorUserId = null,
    DateTimeOffset? From = null,
    DateTimeOffset? To = null,
    int PageNumber = 1,
    int PageSize = 50) : IRequest<PaginatedAuditEntriesDto>;

public record PaginatedAuditEntriesDto(
    IReadOnlyList<AuditEntryDto> Items,
    int TotalCount,
    int PageNumber,
    int PageSize);

public record AuditEntryDto(
    Guid Id,
    string EventType,
    Guid? ActorUserId,
    string Payload,
    DateTimeOffset Created);

public class GetAuthorizationAuditEntriesQueryHandler : IRequestHandler<GetAuthorizationAuditEntriesQuery, PaginatedAuditEntriesDto>
{
    private readonly IApplicationDbContext _context;

    public GetAuthorizationAuditEntriesQueryHandler(IApplicationDbContext context) => _context = context;

    public async Task<PaginatedAuditEntriesDto> Handle(GetAuthorizationAuditEntriesQuery request, CancellationToken cancellationToken)
    {
        var query = _context.AuthorizationAuditEntries.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.EventType))
        {
            query = query.Where(a => a.EventType == request.EventType.Trim());
        }

        if (request.ActorUserId.HasValue)
        {
            query = query.Where(a => a.ActorUserId == request.ActorUserId.Value);
        }

        if (request.From.HasValue)
        {
            query = query.Where(a => a.Created >= request.From.Value);
        }

        if (request.To.HasValue)
        {
            query = query.Where(a => a.Created <= request.To.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var pageNumber = Math.Max(1, request.PageNumber);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);

        var items = await query
            .OrderByDescending(a => a.Created)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(a => new AuditEntryDto(
                a.Id,
                a.EventType,
                a.ActorUserId,
                a.Payload,
                a.Created))
            .ToListAsync(cancellationToken);

        return new PaginatedAuditEntriesDto(items, totalCount, pageNumber, pageSize);
    }
}
