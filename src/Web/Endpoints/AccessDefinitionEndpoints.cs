using qc_authorization.Application.Authorization.Commands.AddRoleToGroup;
using qc_authorization.Application.Authorization.Commands.AssignAuthorizationRoleToUser;
using qc_authorization.Application.Authorization.Commands.AssignPermissionToRole;
using qc_authorization.Application.Authorization.Commands.CreatePermission;
using qc_authorization.Application.Authorization.Commands.CreateRole;
using qc_authorization.Application.Authorization.Commands.CreateRoleGroup;
using qc_authorization.Application.Authorization.Commands.RemovePermissionFromRole;
using qc_authorization.Application.Authorization.Commands.RemoveRoleFromGroup;
using qc_authorization.Application.Authorization.Commands.RevokeAuthorizationRoleFromUser;
using qc_authorization.Application.Authorization.Queries.GetActionCatalogs;
using qc_authorization.Application.Authorization.Queries.GetPermissionById;
using qc_authorization.Application.Authorization.Queries.GetPermissions;
using qc_authorization.Application.Authorization.Queries.GetResourceCatalogs;
using qc_authorization.Application.Authorization.Queries.GetRoleById;
using qc_authorization.Application.Authorization.Queries.GetRoleGroupById;
using qc_authorization.Application.Authorization.Queries.GetRoleGroups;
using qc_authorization.Application.Authorization.Queries.GetRoles;
using qc_authorization.Application.Authorization.Queries.GetUserRoles;
using qc_authorization.Web.Infrastructure;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace qc_authorization.Web.Endpoints;

public class AccessDefinitionEndpoints : IEndpointGroup
{
    public static string? RoutePrefix => "/api/access-definitions";

    public static void Map(RouteGroupBuilder group)
    {
        // Catalogs
        group.MapGet(GetResourceCatalogs, "catalogs/resources");
        group.MapGet(GetActionCatalogs, "catalogs/actions");

        // Permissions
        group.MapGet(GetPermissions, "permissions");
        group.MapGet(GetPermissionById, "permissions/{id:int}");
        group.MapPost(CreatePermission, "permissions");

        // Roles
        group.MapGet(GetRoles, "roles");
        group.MapGet(GetRoleById, "roles/{id:int}");
        group.MapPost(CreateRole, "roles");
        group.MapPost(AssignPermissionToRole, "roles/assign-permission");
        group.MapPost(RemovePermissionFromRole, "roles/remove-permission");

        // Role Groups
        group.MapGet(GetRoleGroups, "role-groups");
        group.MapGet(GetRoleGroupById, "role-groups/{id:int}");
        group.MapPost(CreateRoleGroup, "role-groups");
        group.MapPost(AddRoleToGroup, "role-groups/add-role");
        group.MapPost(RemoveRoleFromGroup, "role-groups/remove-role");

        // User Role Assignments
        group.MapGet(GetUserRoles, "users/{userId:guid}/roles");
        group.MapPost(AssignRoleToUser, "users/assign-role");
        group.MapPost(RevokeRoleFromUser, "users/revoke-role");
    }

    private static async Task<IResult> GetResourceCatalogs([FromQuery] string? searchTerm, ISender sender)
    {
        var result = await sender.Send(new GetResourceCatalogsQuery(searchTerm));
        return Results.Ok(result);
    }

    private static async Task<IResult> GetActionCatalogs([FromQuery] string? searchTerm, ISender sender)
    {
        var result = await sender.Send(new GetActionCatalogsQuery(searchTerm));
        return Results.Ok(result);
    }

    private static async Task<IResult> GetPermissions(
        [FromQuery] string? resource,
        [FromQuery] string? action,
        [FromQuery] string? searchTerm,
        ISender sender)
    {
        var result = await sender.Send(new GetPermissionsQuery(resource, action, searchTerm));
        return Results.Ok(result);
    }

    private static async Task<IResult> GetPermissionById(int id, ISender sender)
    {
        var result = await sender.Send(new GetPermissionByIdQuery(id));
        return Results.Ok(result);
    }

    private static async Task<IResult> CreatePermission(CreatePermissionRequest request, ISender sender)
    {
        var id = await sender.Send(new CreatePermissionCommand(
            request.ResourceCode,
            request.ResourceName,
            request.ActionCode,
            request.ActionName,
            request.Description));

        return Results.Created($"/api/access-definitions/permissions/{id}", new { id });
    }

    private static async Task<IResult> GetRoles([FromQuery] string? searchTerm, ISender sender)
    {
        var result = await sender.Send(new GetRolesQuery(searchTerm));
        return Results.Ok(result);
    }

    private static async Task<IResult> GetRoleById(int id, ISender sender)
    {
        var result = await sender.Send(new GetRoleByIdQuery(id));
        return Results.Ok(result);
    }

    private static async Task<IResult> CreateRole(CreateRoleRequest request, ISender sender)
    {
        var id = await sender.Send(new CreateRoleCommand(request.Code, request.Name, request.Description));
        return Results.Created($"/api/access-definitions/roles/{id}", new { id });
    }

    private static async Task<IResult> AssignPermissionToRole(AssignPermissionToRoleRequest request, ISender sender)
    {
        await sender.Send(new AssignPermissionToRoleCommand(request.RoleId, request.PermissionId));
        return Results.NoContent();
    }

    private static async Task<IResult> RemovePermissionFromRole(RemovePermissionFromRoleRequest request, ISender sender)
    {
        await sender.Send(new RemovePermissionFromRoleCommand(request.RoleId, request.PermissionId));
        return Results.NoContent();
    }

    private static async Task<IResult> GetRoleGroups([FromQuery] string? searchTerm, ISender sender)
    {
        var result = await sender.Send(new GetRoleGroupsQuery(searchTerm));
        return Results.Ok(result);
    }

    private static async Task<IResult> GetRoleGroupById(int id, ISender sender)
    {
        var result = await sender.Send(new GetRoleGroupByIdQuery(id));
        return Results.Ok(result);
    }

    private static async Task<IResult> CreateRoleGroup(CreateRoleGroupRequest request, ISender sender)
    {
        var id = await sender.Send(new CreateRoleGroupCommand(request.Code, request.Name, request.Description));
        return Results.Created($"/api/access-definitions/role-groups/{id}", new { id });
    }

    private static async Task<IResult> AddRoleToGroup(AddRoleToGroupRequest request, ISender sender)
    {
        await sender.Send(new AddRoleToGroupCommand(request.RoleGroupId, request.RoleId));
        return Results.NoContent();
    }

    private static async Task<IResult> RemoveRoleFromGroup(RemoveRoleFromGroupRequest request, ISender sender)
    {
        await sender.Send(new RemoveRoleFromGroupCommand(request.RoleGroupId, request.RoleId));
        return Results.NoContent();
    }

    private static async Task<IResult> GetUserRoles(Guid userId, ISender sender)
    {
        var result = await sender.Send(new GetUserRolesQuery(userId));
        return Results.Ok(result);
    }

    private static async Task<IResult> AssignRoleToUser(AssignRoleToUserRequest request, ISender sender)
    {
        await sender.Send(new AssignAuthorizationRoleToUserCommand(
            request.UserId, request.RoleId, request.ValidFrom, request.ValidTo));
        return Results.NoContent();
    }

    private static async Task<IResult> RevokeRoleFromUser(RevokeRoleFromUserRequest request, ISender sender)
    {
        await sender.Send(new RevokeAuthorizationRoleFromUserCommand(request.UserId, request.RoleId));
        return Results.NoContent();
    }
}

public record CreatePermissionRequest(
    string ResourceCode,
    string ResourceName,
    string ActionCode,
    string ActionName,
    string? Description = null);

public record CreateRoleRequest(string Code, string Name, string? Description = null);

public record AssignPermissionToRoleRequest(int RoleId, int PermissionId);

public record RemovePermissionFromRoleRequest(int RoleId, int PermissionId);

public record CreateRoleGroupRequest(string Code, string Name, string? Description = null);

public record AddRoleToGroupRequest(int RoleGroupId, int RoleId);

public record RemoveRoleFromGroupRequest(int RoleGroupId, int RoleId);

public record AssignRoleToUserRequest(
    Guid UserId,
    int RoleId,
    DateTimeOffset ValidFrom,
    DateTimeOffset? ValidTo = null);

public record RevokeRoleFromUserRequest(Guid UserId, int RoleId);
