using AccessManagement.Application.Organization.Commands.AssignPersonnelToPosition;
using AccessManagement.Application.Organization.Commands.BootstrapSystemAdmin;
using AccessManagement.Application.Organization.Commands.CreatePersonnel;
using AccessManagement.Application.Organization.Commands.CreatePosition;
using AccessManagement.Application.Organization.Commands.EndPositionAssignment;
using AccessManagement.Application.Organization.Commands.LinkPersonnelToIdentityUser;
using AccessManagement.Application.Organization.Commands.ReparentPosition;
using AccessManagement.Application.Organization.Commands.SetPrimaryPositionAssignment;
using AccessManagement.Application.Organization.Queries.GetPersonnel;
using AccessManagement.Application.Organization.Queries.GetPersonnelById;
using AccessManagement.Application.Organization.Queries.GetPositionAssignments;
using AccessManagement.Application.Organization.Queries.GetPositionById;
using AccessManagement.Application.Organization.Queries.GetPositions;
using AccessManagement.Application.Organization.Commands.UpdatePosition;
using AccessManagement.Application.Organization.Queries.GetPositionAuthorizationSummary;
using AccessManagement.Domain.Organization.Enums;
using AccessManagement.WebApi.Infrastructure;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace AccessManagement.WebApi.Endpoints;

public class OrganizationEndpoints : IEndpointGroup
{
    public static string? RoutePrefix => "/api/organization";

    public static void Map(RouteGroupBuilder group)
    {
        // Self-disabling first-admin path. AllowAnonymous is required because no user
        // exists yet to obtain a token; the handler plus bootstrap key are the security gate.
        group.MapPost(BootstrapAdmin, "bootstrap/admin").AllowAnonymous().RequireRateLimiting(UsersEndpoints.AuthRateLimitPolicy);

        // Personnel
        group.MapGet(GetPersonnel, "personnel");
        group.MapGet(GetPersonnelById, "personnel/{id:guid}");
        group.MapPost(CreatePersonnel, "personnel");
        group.MapPost(LinkPersonnelToIdentityUser, "personnel/link-user");

        // Positions
        group.MapGet(GetPositions, "positions");
        group.MapGet(GetPositionById, "positions/{id:guid}");
        group.MapGet(GetPositionAuthorizationSummary, "positions/{id:guid}/authorization-summary");
        group.MapPost(CreatePosition, "positions");
        group.MapPut(UpdatePosition, "positions/{id:guid}");
        group.MapPost(ReparentPosition, "positions/reparent");

        // Assignments
        group.MapGet(GetPositionAssignments, "assignments");
        group.MapPost(AssignPersonnel, "assignments");
        group.MapPost(EndAssignment, "assignments/{id:guid}/end");
        group.MapPost(SetPrimaryAssignment, "assignments/set-primary");
    }

    private static async Task<IResult> BootstrapAdmin(
        BootstrapAdminRequest request,
        ISender sender,
        IHostEnvironment environment,
        IConfiguration configuration,
        HttpContext httpContext)
    {
        if (!environment.IsDevelopment() && !environment.IsEnvironment("Testing"))
        {
            var expected = configuration["Bootstrap:Key"];
            if (string.IsNullOrWhiteSpace(expected))
            {
                return Results.NotFound();
            }

            if (!httpContext.Request.Headers.TryGetValue("X-Bootstrap-Key", out var provided)
                || !CryptographicEquals(provided.ToString(), expected))
            {
                return Results.NotFound();
            }
        }

        var id = await sender.Send(new BootstrapSystemAdminCommand(
            request.NationalId,
            request.FirstName,
            request.LastName,
            request.PersonnelCode,
            request.IdentityUserId));

        return Results.Created($"/api/organization/personnel/{id}", new { id });
    }

    private static bool CryptographicEquals(string left, string right)
    {
        var leftBytes = System.Text.Encoding.UTF8.GetBytes(left);
        var rightBytes = System.Text.Encoding.UTF8.GetBytes(right);
        return leftBytes.Length == rightBytes.Length
            && System.Security.Cryptography.CryptographicOperations.FixedTimeEquals(leftBytes, rightBytes);
    }

    private static async Task<IResult> GetPersonnel(
        [FromQuery] string? searchTerm,
        [FromQuery] PersonnelStatus? status,
        [FromQuery] bool? hasIdentityUser,
        [FromQuery] int pageNumber,
        [FromQuery] int pageSize,
        ISender sender)
    {
        var result = await sender.Send(new GetPersonnelQuery(
            searchTerm,
            status,
            hasIdentityUser,
            pageNumber == 0 ? 1 : pageNumber,
            pageSize == 0 ? 50 : pageSize));
        return Results.Ok(result);
    }

    private static async Task<IResult> GetPersonnelById(Guid id, ISender sender)
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
        [FromQuery] Guid? companyId,
        [FromQuery] string? searchTerm,
        [FromQuery] Guid? parentPositionId,
        ISender sender)
    {
        var result = await sender.Send(new GetPositionsQuery(companyId, searchTerm, parentPositionId));
        return Results.Ok(result);
    }

    private static async Task<IResult> GetPositionById(Guid id, ISender sender)
    {
        var result = await sender.Send(new GetPositionByIdQuery(id));
        return Results.Ok(result);
    }

    private static async Task<IResult> GetPositionAuthorizationSummary(Guid id, ISender sender)
    {
        var result = await sender.Send(new GetPositionAuthorizationSummaryQuery(id));
        return Results.Ok(result);
    }

    private static async Task<IResult> UpdatePosition(Guid id, UpdatePositionRequest request, ISender sender)
    {
        await sender.Send(new UpdatePositionCommand(id, request.Title, request.Description, request.Status));
        return Results.NoContent();
    }

    private static async Task<IResult> CreatePosition(CreatePositionRequest request, ISender sender)
    {
        var id = await sender.Send(new CreatePositionCommand(
            request.CompanyUnitId,
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
        [FromQuery] Guid? personnelId,
        [FromQuery] Guid? positionId,
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

    private static async Task<IResult> EndAssignment(Guid id, EndAssignmentRequest? request, ISender sender)
    {
        await sender.Send(new EndPositionAssignmentCommand(id, request?.EndedAt));
        return Results.NoContent();
    }

    private static async Task<IResult> SetPrimaryAssignment(SetPrimaryAssignmentRequest request, ISender sender)
    {
        await sender.Send(new SetPrimaryPositionAssignmentCommand(request.PersonnelId, request.AssignmentId));
        return Results.NoContent();
    }
}

public record BootstrapAdminRequest(
    string NationalId,
    string FirstName,
    string LastName,
    string PersonnelCode,
    Guid IdentityUserId);

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
    Guid CompanyUnitId,
    string Code,
    string Title,
    Guid? ParentPositionId = null,
    string? Description = null);

public record AssignPersonnelRequest(
    Guid PersonnelId,
    Guid PositionId,
    DateTimeOffset EffectiveFrom,
    DateTimeOffset? EffectiveTo = null);

public record ReparentPositionRequest(Guid PositionId, Guid? NewParentPositionId);

public record LinkPersonnelRequest(Guid PersonnelId, Guid IdentityUserId);

public record EndAssignmentRequest(DateTimeOffset? EndedAt = null);

public record SetPrimaryAssignmentRequest(Guid PersonnelId, Guid AssignmentId);

public record UpdatePositionRequest(string Title, string? Description, PositionStatus Status);
