using qc_authorization.Application.Authorization.Commands.CreateGrant;
using qc_authorization.Application.Authorization.Queries.EvaluateAccess;
using qc_authorization.Application.Common.Interfaces;
using qc_authorization.Domain.Authorization.Enums;
using qc_authorization.Domain.Authorization.ValueObjects;
using qc_authorization.Web.Infrastructure;
using MediatR;

namespace qc_authorization.Web.Endpoints;

public class AuthorizationEndpoints : IEndpointGroup
{
    public static void Map(RouteGroupBuilder group)
    {
        group.MapPost(CreateGrant);
        group.MapPost(EvaluateAccess);
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

        return Results.Created($"/api/AuthorizationEndpoints/grants/{id}", new { id });
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
