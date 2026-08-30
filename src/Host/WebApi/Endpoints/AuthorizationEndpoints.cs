using AccessManagement.Application.Authorization.Commands.EvaluateAccessBatch;
using AccessManagement.Application.Authorization.Commands.GrantAccess;
using AccessManagement.Application.Authorization.Commands.RevokeAccess;
using AccessManagement.Application.Authorization.Queries.EvaluateAccess;
using AccessManagement.Application.Authorization.Queries.EvaluateAccessForSubject;
using AccessManagement.Application.Authorization.Queries.GetAccessibleScopes;
using AccessManagement.Application.Authorization.Queries.GetGrantById;
using AccessManagement.Application.Authorization.Queries.GetGrantTargets;
using AccessManagement.Application.Authorization.Queries.GetGrants;
using AccessManagement.Application.Authorization.Services;
using AccessManagement.Application.Common.Interfaces;
using AccessManagement.Domain.Authorization.Enums;
using AccessManagement.WebApi.Infrastructure;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace AccessManagement.WebApi.Endpoints;

public class AuthorizationEndpoints : IEndpointGroup
{
    public static string? RoutePrefix => "/api/authorization";

    public static void Map(RouteGroupBuilder group)
    {
        group.MapGet(GetGrants, "grants");
        group.MapGet(GetGrantById, "grants/{id:guid}");
        group.MapPost(GrantAccess, "access-grants");
        group.MapPost(RevokeAccess, "access-grants/revoke");
        group.MapGet(GetGrantTargets, "access-targets");

        group.MapPost(EvaluateAccess, "evaluate");
        group.MapPost(EvaluateAccessBatch, "evaluate-batch");
        group.MapPost(GetAccessibleScopes, "accessible-scopes");
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

    private static async Task<IResult> GrantAccess(
        GrantAccessRequest request,
        ISender sender,
        ICurrentUser currentUser)
    {
        if (currentUser.UserId is not Guid actorUserId)
        {
            return Results.Unauthorized();
        }

        if (currentUser.ActiveCompanyId is not Guid actorCompanyUnitId)
        {
            return Results.BadRequest(new { message = "An active company workspace is required." });
        }

        var id = await sender.Send(new GrantAccessCommand(
            actorUserId,
            actorCompanyUnitId,
            request.TargetKind,
            request.TargetId,
            request.PermissionId,
            request.ScopeUnitId,
            request.ValidFrom,
            request.ValidTo));

        return Results.Created($"/api/authorization/grants/{id}", new { id });
    }

    private static async Task<IResult> RevokeAccess(
        RevokeAccessRequest request,
        ISender sender,
        ICurrentUser currentUser)
    {
        if (currentUser.UserId is not Guid actorUserId)
        {
            return Results.Unauthorized();
        }

        if (currentUser.ActiveCompanyId is not Guid actorCompanyUnitId)
        {
            return Results.BadRequest(new { message = "An active company workspace is required." });
        }

        await sender.Send(new RevokeAccessCommand(
            actorUserId,
            actorCompanyUnitId,
            request.TargetKind,
            request.TargetId,
            request.PermissionId,
            request.ScopeUnitId));

        return Results.NoContent();
    }

    private static async Task<IResult> GetGrantTargets(
        ISender sender,
        ICurrentUser currentUser)
    {
        if (currentUser.UserId is not Guid actorUserId)
        {
            return Results.Unauthorized();
        }

        var companyId = currentUser.ActiveCompanyId;
        if (companyId is not Guid actorCompanyUnitId)
        {
            return Results.BadRequest(new { message = "An active company workspace is required." });
        }

        var result = await sender.Send(new GetGrantTargetsQuery(actorUserId, actorCompanyUnitId));
        return Results.Ok(result);
    }

    private static async Task<IResult> EvaluateAccess(
        EvaluateAccessRequest request,
        ISender sender,
        ICurrentUser currentUser)
    {
        if (currentUser.UserId is not Guid userId)
        {
            return Results.Unauthorized();
        }

        var result = await sender.Send(new EvaluateAccessQuery(
            userId,
            request.PermissionCode,
            request.ActivePositionId,
            request.ResourceScopeUnitId,
            request.When));

        return Results.Ok(result);
    }

    private static async Task<IResult> EvaluateAccessBatch(
        EvaluateAccessBatchRequest request,
        ISender sender)
    {
        var result = await sender.Send(new EvaluateAccessBatchCommand(request.Rows));
        return Results.Ok(result);
    }

    private static async Task<IResult> GetAccessibleScopes(
        AccessibleScopesRequest request,
        ISender sender,
        ICurrentUser currentUser)
    {
        if (currentUser.UserId is not Guid userId)
        {
            return Results.Unauthorized();
        }

        var result = await sender.Send(new GetAccessibleScopesQuery(
            userId,
            request.ActivePositionId,
            request.PermissionCode,
            currentUser.ActiveCompanyId));

        return Results.Ok(result);
    }

    private static async Task<IResult> SimulateEvaluation(
        SimulateEvaluationRequest request,
        ISender sender,
        ICurrentUser currentUser,
        IActorAccessService actorAccess)
    {
        if (currentUser.UserId is not Guid actorUserId)
        {
            return Results.Unauthorized();
        }

        if (!await actorAccess.IsUserAdminAsync(actorUserId, currentUser.ActiveCompanyId))
        {
            return Results.Forbid();
        }

        var result = await sender.Send(new EvaluateAccessForSubjectQuery(
            request.UserId,
            request.PermissionCode,
            request.ActivePositionId,
            request.ResourceScopeUnitId,
            request.When));

        return Results.Ok(result);
    }
}

public record GrantAccessRequest(
    AccessGrantTargetKind TargetKind,
    Guid TargetId,
    Guid PermissionId,
    Guid? ScopeUnitId,
    DateTimeOffset ValidFrom,
    DateTimeOffset? ValidTo);

public record RevokeAccessRequest(
    AccessGrantTargetKind TargetKind,
    Guid TargetId,
    Guid PermissionId,
    Guid? ScopeUnitId);

public record EvaluateAccessRequest(
    string PermissionCode,
    Guid? ActivePositionId = null,
    Guid? ResourceScopeUnitId = null,
    DateTimeOffset? When = null);

public record AccessibleScopesRequest(
    string PermissionCode,
    Guid? ActivePositionId = null);

public record SimulateEvaluationRequest(
    Guid UserId,
    string PermissionCode,
    Guid? ActivePositionId = null,
    Guid? ResourceScopeUnitId = null,
    DateTimeOffset? When = null);

public record EvaluateAccessBatchRequest(IReadOnlyList<EvaluateAccessBatchItem> Rows);
