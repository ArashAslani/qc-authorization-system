using Microsoft.AspNetCore.Identity;
using qc_authorization.Application.Common.Interfaces;
using qc_authorization.Application.Organization.Commands.LinkPersonnelToIdentityUser;
using qc_authorization.Infrastructure.Identity;
using qc_authorization.Web.Infrastructure;
using MediatR;

namespace qc_authorization.Web.Endpoints;

public class UsersEndpoints : IEndpointGroup
{
    public static string? RoutePrefix => "/api/Users";

    public static void Map(RouteGroupBuilder group)
    {
        group.MapPost(Register);
        group.MapPost(Login);
    }

    private static async Task<IResult> Register(
        RegisterRequest request,
        UserManager<ApplicationUser> userManager,
        IPersonnelIdentityBridge personnelIdentityBridge,
        JwtTokenService jwtTokenService)
    {
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = request.Email.Trim(),
            Email = request.Email.Trim(),
            EmailConfirmed = true,
        };

        var createResult = await userManager.CreateAsync(user, request.Password);
        if (!createResult.Succeeded)
        {
            return Results.ValidationProblem(createResult.Errors.ToDictionary(
                e => e.Code,
                e => new[] { e.Description }));
        }

        if (request.PersonnelId.HasValue)
        {
            await personnelIdentityBridge.LinkAsync(
                request.PersonnelId.Value,
                user.Id,
                CancellationToken.None);

            user = (await userManager.FindByIdAsync(user.Id.ToString()))!;
        }

        var token = jwtTokenService.GenerateToken(user);
        return Results.Ok(new AuthResponse(token, user.Id, user.PersonnelId));
    }

    private static async Task<IResult> Login(
        LoginRequest request,
        UserManager<ApplicationUser> userManager,
        JwtTokenService jwtTokenService)
    {
        var user = await userManager.FindByEmailAsync(request.Email.Trim());
        if (user is null || !await userManager.CheckPasswordAsync(user, request.Password))
        {
            throw new UnauthorizedAccessException("Invalid email or password.");
        }

        var token = jwtTokenService.GenerateToken(user);
        return Results.Ok(new AuthResponse(token, user.Id, user.PersonnelId));
    }
}

public record RegisterRequest(string Email, string Password, int? PersonnelId = null);

public record LoginRequest(string Email, string Password);

public record AuthResponse(string Token, Guid UserId, int? PersonnelId);
