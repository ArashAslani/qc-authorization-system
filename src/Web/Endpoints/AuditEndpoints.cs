using qc_authorization.Application.Authorization.Audit.Queries.GetAuditEntries;
using qc_authorization.Web.Infrastructure;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace qc_authorization.Web.Endpoints;

public class AuditEndpoints : IEndpointGroup
{
    public static string? RoutePrefix => "/api/audit";

    public static void Map(RouteGroupBuilder group)
    {
        group.MapGet(GetAuditEntries, "entries");
    }

    private static async Task<IResult> GetAuditEntries(
        [FromQuery] string? eventType,
        [FromQuery] Guid? actorUserId,
        [FromQuery] DateTimeOffset? from,
        [FromQuery] DateTimeOffset? to,
        [FromQuery] int? pageNumber,
        [FromQuery] int? pageSize,
        ISender sender)
    {
        var result = await sender.Send(new GetAuthorizationAuditEntriesQuery(
            eventType,
            actorUserId,
            from,
            to,
            pageNumber ?? 1,
            pageSize ?? 50));

        return Results.Ok(result);
    }
}
