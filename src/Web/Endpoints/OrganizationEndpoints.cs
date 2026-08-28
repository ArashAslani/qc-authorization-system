using qc_authorization.Application.Organization.Commands.AssignPersonnelToPosition;
using qc_authorization.Application.Organization.Commands.CreatePersonnel;
using qc_authorization.Application.Organization.Commands.CreatePosition;
using qc_authorization.Application.Organization.Commands.LinkPersonnelToIdentityUser;
using qc_authorization.Application.Organization.Commands.ReparentPosition;
using qc_authorization.Application.Organization.Queries.GetPersonnel;
using qc_authorization.Application.Organization.Queries.GetPersonnelById;
using qc_authorization.Application.Organization.Queries.GetPositionAssignments;
using qc_authorization.Application.Organization.Queries.GetPositionById;
using qc_authorization.Application.Organization.Queries.GetPositions;
using qc_authorization.Domain.Organization.Enums;
using qc_authorization.Web.Infrastructure;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace qc_authorization.Web.Endpoints;

public class OrganizationEndpoints : IEndpointGroup
{
    public static string? RoutePrefix => "/api/organization";

    public static void Map(RouteGroupBuilder group)
    {
        // Personnel
        group.MapGet(GetPersonnel, "personnel");
        group.MapGet(GetPersonnelById, "personnel/{id:int}");
        group.MapPost(CreatePersonnel, "personnel");
        group.MapPost(LinkPersonnelToIdentityUser, "personnel/link-user");

        // Positions
        group.MapGet(GetPositions, "positions");
        group.MapGet(GetPositionById, "positions/{id:int}");
        group.MapPost(CreatePosition, "positions");
        group.MapPost(ReparentPosition, "positions/reparent");

        // Assignments
        group.MapGet(GetPositionAssignments, "assignments");
        group.MapPost(AssignPersonnel, "assignments");
    }

    private static async Task<IResult> GetPersonnel(
        [FromQuery] string? searchTerm,
        [FromQuery] PersonnelStatus? status,
        [FromQuery] bool? hasIdentityUser,
        ISender sender)
    {
        var result = await sender.Send(new GetPersonnelQuery(searchTerm, status, hasIdentityUser));
        return Results.Ok(result);
    }

    private static async Task<IResult> GetPersonnelById(int id, ISender sender)
    {
        var result = await sender.Send(new GetPersonnelByIdQuery(id));
        return Results.Ok(result);
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
            request.IdentityUserId));

        return Results.Created($"/api/organization/personnel/{id}", new { id });
    }

    private static async Task<IResult> LinkPersonnelToIdentityUser(LinkPersonnelRequest request, ISender sender)
    {
        await sender.Send(new LinkPersonnelToIdentityUserCommand(request.PersonnelId, request.IdentityUserId));
        return Results.NoContent();
    }

    private static async Task<IResult> GetPositions(
        [FromQuery] int? companyId,
        [FromQuery] string? searchTerm,
        [FromQuery] int? parentPositionId,
        ISender sender)
    {
        var result = await sender.Send(new GetPositionsQuery(companyId, searchTerm, parentPositionId));
        return Results.Ok(result);
    }

    private static async Task<IResult> GetPositionById(int id, ISender sender)
    {
        var result = await sender.Send(new GetPositionByIdQuery(id));
        return Results.Ok(result);
    }

    private static async Task<IResult> CreatePosition(CreatePositionRequest request, ISender sender)
    {
        var id = await sender.Send(new CreatePositionCommand(
            request.CompanyId,
            request.Code,
            request.Title,
            request.Description,
            request.ParentPositionId));

        return Results.Created($"/api/organization/positions/{id}", new { id });
    }

    private static async Task<IResult> ReparentPosition(ReparentPositionRequest request, ISender sender)
    {
        await sender.Send(new ReparentPositionCommand(request.PositionId, request.NewParentPositionId));
        return Results.NoContent();
    }

    private static async Task<IResult> GetPositionAssignments(
        [FromQuery] int? personnelId,
        [FromQuery] int? positionId,
        [FromQuery] bool? activeOnly,
        ISender sender)
    {
        var result = await sender.Send(new GetPositionAssignmentsQuery(personnelId, positionId, activeOnly));
        return Results.Ok(result);
    }

    private static async Task<IResult> AssignPersonnel(AssignPersonnelRequest request, ISender sender)
    {
        var id = await sender.Send(new AssignPersonnelToPositionCommand(
            request.PersonnelId,
            request.PositionId,
            request.EffectiveFrom,
            request.EffectiveTo));

        return Results.Created($"/api/organization/assignments/{id}", new { id });
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
    Guid? IdentityUserId = null);

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

public record LinkPersonnelRequest(int PersonnelId, Guid IdentityUserId);
