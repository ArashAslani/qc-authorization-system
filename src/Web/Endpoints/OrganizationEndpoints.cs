using qc_authorization.Application.Authorization.Commands.CreateDelegation;
using qc_authorization.Application.Authorization.Commands.CreateGrant;
using qc_authorization.Application.Authorization.Commands.CreatePermission;
using qc_authorization.Application.Authorization.Commands.CreateRole;
using qc_authorization.Application.Authorization.Commands.RevokeDelegation;
using qc_authorization.Application.Authorization.Queries.EvaluateAccess;
using qc_authorization.Application.Organization.Commands.AssignPersonnelToPosition;
using qc_authorization.Application.Organization.Commands.CreatePersonnel;
using qc_authorization.Application.Organization.Commands.CreatePosition;
using qc_authorization.Application.Organization.Commands.ReparentPosition;
using qc_authorization.Domain.Authorization.Enums;
using qc_authorization.Domain.Authorization.ValueObjects;
using qc_authorization.Domain.Organization.Enums;
using qc_authorization.Web.Infrastructure;
using MediatR;

namespace qc_authorization.Web.Endpoints;

public class OrganizationEndpoints : IEndpointGroup
{
    public static void Map(RouteGroupBuilder group)
    {
        group.MapPost(CreatePersonnel);
        group.MapPost(CreatePosition);
        group.MapPost(AssignPersonnel);
        group.MapPost(ReparentPosition);
    }

    private static async Task<IResult> CreatePersonnel(CreatePersonnelRequest request, ISender sender)
    {
        var id = await sender.Send(new CreatePersonnelCommand(
            request.NationalId,
            request.FirstName,
            request.LastName,
            request.PersonalCode,
            request.PhoneNumber,
            request.Gender,
            request.Status,
            request.SystemUserId));

        return Results.Created($"/api/OrganizationEndpoints/personnel/{id}", new { id });
    }

    private static async Task<IResult> CreatePosition(CreatePositionRequest request, ISender sender)
    {
        var id = await sender.Send(new CreatePositionCommand(
            request.CompanyId,
            request.Code,
            request.Title,
            request.Description,
            request.ParentPositionId));

        return Results.Created($"/api/OrganizationEndpoints/positions/{id}", new { id });
    }

    private static async Task<IResult> AssignPersonnel(AssignPersonnelRequest request, ISender sender)
    {
        var id = await sender.Send(new AssignPersonnelToPositionCommand(
            request.PersonnelId,
            request.PositionId,
            request.EffectiveFrom,
            request.EffectiveTo));

        return Results.Created($"/api/OrganizationEndpoints/assignments/{id}", new { id });
    }

    private static async Task<IResult> ReparentPosition(ReparentPositionRequest request, ISender sender)
    {
        await sender.Send(new ReparentPositionCommand(request.PositionId, request.NewParentPositionId));
        return Results.NoContent();
    }
}

public record CreatePersonnelRequest(
    string NationalId,
    string FirstName,
    string LastName,
    string PersonalCode,
    string? PhoneNumber = null,
    PersonnelGender Gender = PersonnelGender.Unknown,
    PersonnelStatus Status = PersonnelStatus.Active,
    int? SystemUserId = null);

public record CreatePositionRequest(
    int CompanyId,
    string Code,
    string Title,
    int? ParentPositionId = null,
    string? Description = null);

public record AssignPersonnelRequest(
    int PersonnelId,
    int PositionId,
    DateTimeOffset EffectiveFrom,
    DateTimeOffset? EffectiveTo = null);

public record ReparentPositionRequest(int PositionId, int? NewParentPositionId);
