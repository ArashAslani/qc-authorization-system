using qc_authorization.Application.Authorization.Commands.AddRoleToGroup;
using qc_authorization.Application.Authorization.Commands.AssignPermissionToRole;
using qc_authorization.Application.Authorization.Commands.CreatePermission;
using qc_authorization.Application.Authorization.Commands.CreateRole;
using qc_authorization.Application.Authorization.Commands.CreateRoleGroup;
using qc_authorization.Web.Infrastructure;
using MediatR;

namespace qc_authorization.Web.Endpoints;

public class AccessDefinitionEndpoints : IEndpointGroup
{
    public static void Map(RouteGroupBuilder group)
    {
        group.MapPost(CreatePermission);
        group.MapPost(CreateRole);
        group.MapPost(AssignPermissionToRole);
        group.MapPost(CreateRoleGroup);
        group.MapPost(AddRoleToGroup);
    }

    private static async Task<IResult> CreatePermission(CreatePermissionRequest request, ISender sender)
    {
        var id = await sender.Send(new CreatePermissionCommand(
            request.ResourceCode,
            request.ResourceName,
            request.ActionCode,
            request.ActionName,
            request.Description));

        return Results.Created($"/api/AccessDefinitionEndpoints/permissions/{id}", new { id });
    }

    private static async Task<IResult> CreateRole(CreateRoleRequest request, ISender sender)
    {
        var id = await sender.Send(new CreateRoleCommand(request.Code, request.Name, request.Description));
        return Results.Created($"/api/AccessDefinitionEndpoints/roles/{id}", new { id });
    }

    private static async Task<IResult> AssignPermissionToRole(AssignPermissionToRoleRequest request, ISender sender)
    {
        await sender.Send(new AssignPermissionToRoleCommand(request.RoleId, request.PermissionId));
        return Results.NoContent();
    }

    private static async Task<IResult> CreateRoleGroup(CreateRoleGroupRequest request, ISender sender)
    {
        var id = await sender.Send(new CreateRoleGroupCommand(request.Code, request.Name, request.Description));
        return Results.Created($"/api/AccessDefinitionEndpoints/role-groups/{id}", new { id });
    }

    private static async Task<IResult> AddRoleToGroup(AddRoleToGroupRequest request, ISender sender)
    {
        await sender.Send(new AddRoleToGroupCommand(request.RoleGroupId, request.RoleId));
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

public record CreateRoleGroupRequest(string Code, string Name, string? Description = null);

public record AddRoleToGroupRequest(int RoleGroupId, int RoleId);
