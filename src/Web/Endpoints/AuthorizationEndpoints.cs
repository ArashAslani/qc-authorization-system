using qc_authorization.Application.Authorization.Commands.CreateGrant;
using qc_authorization.Application.Authorization.Queries.EvaluateAccess;
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

    private static async Task<IResult> EvaluateAccess(EvaluateAccessRequest request, ISender sender)
    {
        var result = await sender.Send(new EvaluateAccessQuery(
            request.SubjectType,
            request.SubjectId,
            request.Action,
            request.Resource,
            request.ResourceId,
            request.When));

        return Results.Ok(result);
    }
}

public record CreateGrantRequest(
    SubjectType SubjectType,
    int SubjectId,
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
    SubjectType SubjectType,
    int SubjectId,
    string Action,
    string Resource,
    string? ResourceId,
    DateTimeOffset When);
