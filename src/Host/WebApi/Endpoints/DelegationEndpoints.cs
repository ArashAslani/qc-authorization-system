using AccessManagement.Application.Authorization.Commands.CreateDelegation;
using AccessManagement.Application.Authorization.Commands.RevokeDelegation;
using AccessManagement.Application.Authorization.Queries.GetDelegationById;
using AccessManagement.Application.Authorization.Queries.GetDelegations;
using AccessManagement.Domain.Authorization.ValueObjects;
using AccessManagement.WebApi.Infrastructure;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace AccessManagement.WebApi.Endpoints;

public class DelegationEndpoints : IEndpointGroup
{
    public static string? RoutePrefix => "/api/delegations";

    public static void Map(RouteGroupBuilder group)
    {
        group.MapGet(GetDelegations);
        group.MapGet(GetDelegationById, "{id:guid}");
        group.MapPost(CreateDelegation);
        group.MapPost(RevokeDelegation, "{id:guid}/revoke");
    }

    private static async Task<IResult> GetDelegations(
        [FromQuery] Guid? delegatorUserId,
        [FromQuery] Guid? delegateUserId,
        [FromQuery] Guid? permissionId,
        [FromQuery] bool? activeOnly,
        ISender sender)
    {
        var result = await sender.Send(new GetDelegationsQuery(
            delegatorUserId,
            delegateUserId,
            permissionId,
            activeOnly));

        return Results.Ok(result);
    }

    private static async Task<IResult> GetDelegationById(Guid id, ISender sender)
    {
        var result = await sender.Send(new GetDelegationByIdQuery(id));
        return Results.Ok(result);
    }

    private static async Task<IResult> CreateDelegation(CreateDelegationRequest request, ISender sender)
    {
        var id = await sender.Send(new CreateDelegationCommand(
            request.DelegatorUserId,
            request.DelegateUserId,
            request.PermissionId,
            request.ValidFrom,
            request.ValidTo,
            request.ScopeUnitId,
            request.Delegable,
            request.ParentDelegationId));

        return Results.Created($"/api/delegations/{id}", new { id });
    }

    private static async Task<IResult> RevokeDelegation(Guid id, ISender sender)
    {
        await sender.Send(new RevokeDelegationCommand(id));
        return Results.NoContent();
    }
}

public record CreateDelegationRequest(
    Guid DelegatorUserId,
    Guid DelegateUserId,
    Guid PermissionId,
    DateTimeOffset ValidFrom,
    DateTimeOffset? ValidTo,
    Guid? ScopeUnitId = null,
    bool Delegable = true,
    Guid? ParentDelegationId = null);
