using qc_authorization.Application.Authorization.Commands.CreateDelegation;
using qc_authorization.Application.Authorization.Commands.RevokeDelegation;
using qc_authorization.Domain.Authorization.ValueObjects;
using qc_authorization.Web.Infrastructure;
using MediatR;

namespace qc_authorization.Web.Endpoints;

public class DelegationEndpoints : IEndpointGroup
{
    public static void Map(RouteGroupBuilder group)
    {
        group.MapPost(CreateDelegation);
        group.MapPost(RevokeDelegation, "{id}/revoke");
    }

    private static async Task<IResult> CreateDelegation(CreateDelegationRequest request, ISender sender)
    {
        var id = await sender.Send(new CreateDelegationCommand(
            request.DelegatorUserId,
            request.DelegateUserId,
            request.PermissionId,
            request.ValidFrom,
            request.ValidTo,
            request.ScopeKind,
            request.ScopeIdentifier,
            request.Delegable,
            request.ParentDelegationId));

        return Results.Created($"/api/DelegationEndpoints/{id}", new { id });
    }

    private static async Task<IResult> RevokeDelegation(int id, ISender sender)
    {
        await sender.Send(new RevokeDelegationCommand(id));
        return Results.NoContent();
    }
}

public record CreateDelegationRequest(
    Guid DelegatorUserId,
    Guid DelegateUserId,
    int PermissionId,
    DateTimeOffset ValidFrom,
    DateTimeOffset? ValidTo,
    ScopeKind ScopeKind = ScopeKind.Unbounded,
    string? ScopeIdentifier = null,
    bool Delegable = true,
    int? ParentDelegationId = null);
