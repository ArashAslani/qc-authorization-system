using AccessManagement.Application.Authorization.Commands.CreateGrant;
using AccessManagement.Application.Authorization.Commands.RevokeGrant;
using AccessManagement.Application.Authorization.Queries.EvaluateAccess;
using AccessManagement.Application.Authorization.Queries.EvaluateAccessForSubject;
using AccessManagement.Application.Authorization.Queries.GetGrantById;
using AccessManagement.Application.Authorization.Queries.GetGrants;
using AccessManagement.Application.Common.Interfaces;
using AccessManagement.Domain.Authorization.Enums;
using AccessManagement.Domain.Authorization.ValueObjects;
using AccessManagement.WebApi.Infrastructure;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace AccessManagement.WebApi.Endpoints;

public class AuthorizationEndpoints : IEndpointGroup
{
    public static string? RoutePrefix => "/api/authorization";

    public static void Map(RouteGroupBuilder group)
    {
        // Grants
        group.MapGet(GetGrants, "grants");
        group.MapGet(GetGrantById, "grants/{id:guid}");
        group.MapPost(CreateGrant, "grants");
        group.MapPost(RevokeGrant, "grants/{id:guid}/revoke");

        // Evaluation
        group.MapPost(EvaluateAccess, "evaluate");
        group.MapPost(SimulateEvaluation, "simulate-evaluation");
    }

    private static async Task<IResult> GetGrants(
        [FromQuery] SubjectType? subjectType,
        [FromQuery] Guid? subjectId,
        [FromQuery] Guid? subjectUserId,
        [FromQuery] Guid? permissionId,
        [FromQuery] Effect? effect,
        [FromQuery] SourceType? sourceType,
        [FromQuery] bool? activeOnly,
        ISender sender)
    {
        var result = await sender.Send(new GetGrantsQuery(
            subjectType,
            subjectId,
            subjectUserId,
            permissionId,
            effect,
            sourceType,
            activeOnly));

        return Results.Ok(result);
    }

    private static async Task<IResult> GetGrantById(Guid id, ISender sender)
    {
        var result = await sender.Send(new GetGrantByIdQuery(id));
        return Results.Ok(result);
    }

    private static async Task<IResult> CreateGrant(CreateGrantRequest request, ISender sender)
    {
        var id = await sender.Send(new CreateGrantCommand(
            request.SubjectType,
            request.SubjectId,
            request.SubjectUserId,
            request.PermissionId,
            request.Resource,
            request.ResourceId,
            request.ScopeUnitId,
            request.Effect,
            request.SourceType,
            request.SourceId,
            request.ValidFrom,
            request.ValidTo,
            request.Priority));

        return Results.Created($"/api/authorization/grants/{id}", new { id });
    }

    private static async Task<IResult> RevokeGrant(Guid id, ISender sender)
    {
        await sender.Send(new RevokeGrantCommand(id));
        return Results.NoContent();
    }

    private static async Task<IResult> EvaluateAccess(
        EvaluateAccessRequest request,
        ISender sender,
        ICurrentUser currentUser)
    {
        if (!currentUser.IsAuthenticated || currentUser.UserId is null)
        {
            return Results.Unauthorized();
        }

        var query = new EvaluateAccessQuery(
            SubjectType.User,
            Guid.Empty,
            currentUser.UserId,
            request.Action,
            request.Resource,
            request.ResourceId,
            request.When);

        var result = await sender.Send(query);
        return Results.Ok(result);
    }

    private static async Task<IResult> SimulateEvaluation(
        SimulateEvaluationRequest request,
        ISender sender)
    {
        var query = new EvaluateAccessForSubjectQuery(
            request.SubjectType,
            request.SubjectId,
            request.UserId,
            request.Action,
            request.Resource,
            request.ResourceId,
            request.When);

        var result = await sender.Send(query);
        return Results.Ok(result);
    }
}

public record CreateGrantRequest(
    SubjectType SubjectType,
    Guid SubjectId,
    Guid? SubjectUserId,
    Guid PermissionId,
    string? Resource,
    string? ResourceId,
    Guid? ScopeUnitId,
    Effect Effect,
    SourceType SourceType,
    Guid SourceId,
    DateTimeOffset ValidFrom,
    DateTimeOffset? ValidTo,
    int Priority);

public record EvaluateAccessRequest(
    string Action,
    string Resource,
    string? ResourceId,
    DateTimeOffset When);

public record SimulateEvaluationRequest(
    SubjectType SubjectType,
    Guid SubjectId,
    Guid? UserId,
    string Action,
    string Resource,
    string? ResourceId,
    DateTimeOffset When);
