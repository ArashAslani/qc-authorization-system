using qc_authorization.Application.Authorization.Commands.CreateGrant;
using qc_authorization.Application.Authorization.Commands.RevokeGrant;
using qc_authorization.Application.Authorization.Queries.EvaluateAccess;
using qc_authorization.Application.Authorization.Queries.EvaluateAccessForSubject;
using qc_authorization.Application.Authorization.Queries.GetGrantById;
using qc_authorization.Application.Authorization.Queries.GetGrants;
using qc_authorization.Application.Common.Interfaces;
using qc_authorization.Domain.Authorization.Enums;
using qc_authorization.Domain.Authorization.ValueObjects;
using qc_authorization.Web.Infrastructure;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace qc_authorization.Web.Endpoints;

public class AuthorizationEndpoints : IEndpointGroup
{
    public static string? RoutePrefix => "/api/authorization";

    public static void Map(RouteGroupBuilder group)
    {
        // Grants
        group.MapGet(GetGrants, "grants");
        group.MapGet(GetGrantById, "grants/{id:int}");
        group.MapPost(CreateGrant, "grants");
        group.MapPost(RevokeGrant, "grants/{id:int}/revoke");

        // Evaluation
        group.MapPost(EvaluateAccess, "evaluate");
        group.MapPost(SimulateEvaluation, "simulate-evaluation");
    }

    private static async Task<IResult> GetGrants(
        [FromQuery] SubjectType? subjectType,
        [FromQuery] int? subjectId,
        [FromQuery] Guid? subjectUserId,
        [FromQuery] int? permissionId,
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

    private static async Task<IResult> GetGrantById(int id, ISender sender)
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
            request.ScopeKind,
            request.ScopeIdentifier,
            request.Effect,
            request.SourceType,
            request.SourceId,
            request.ValidFrom,
            request.ValidTo,
            request.Priority));

        return Results.Created($"/api/authorization/grants/{id}", new { id });
    }

    private static async Task<IResult> RevokeGrant(int id, ISender sender)
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
            0,
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
    int SubjectId,
    Guid? SubjectUserId,
    int PermissionId,
    string? Resource,
    string? ResourceId,
    ScopeKind ScopeKind,
    string? ScopeIdentifier,
    Effect Effect,
    SourceType SourceType,
    int SourceId,
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
    int SubjectId,
    Guid? UserId,
    string Action,
    string Resource,
    string? ResourceId,
    DateTimeOffset When);
