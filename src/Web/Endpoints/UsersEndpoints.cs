using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using qc_authorization.Application.Common.Interfaces;
using qc_authorization.Infrastructure.Data;
using qc_authorization.Infrastructure.Identity;
using qc_authorization.Web.Infrastructure;

namespace qc_authorization.Web.Endpoints;

public class UsersEndpoints : IEndpointGroup
{
    public static string? RoutePrefix => "/api/users";

    public static void Map(RouteGroupBuilder group)
    {
        group.MapGet(GetUsers);
        group.MapGet(GetUserById, "{id:guid}");
        group.MapPost(Register, "register");
        group.MapPost(Login, "login");
    }

    private static async Task<IResult> GetUsers(
        [FromQuery] string? searchTerm,
        [FromQuery] bool? hasPersonnel,
        ApplicationDbContext context)
    {
        var query = context.Users.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.Trim().ToLower();
            query = query.Where(u =>
                (u.Email != null && u.Email.ToLower().Contains(term)) ||
                (u.UserName != null && u.UserName.ToLower().Contains(term)));
        }

        if (hasPersonnel.HasValue)
        {
            query = hasPersonnel.Value
                ? query.Where(u => u.PersonnelId != null)
                : query.Where(u => u.PersonnelId == null);
        }

        var users = await query
            .OrderBy(u => u.Email)
            .Select(u => new UserSummaryDto(
                u.Id,
                u.UserName,
                u.Email,
                u.EmailConfirmed,
                u.PersonnelId,
                u.LockoutEnabled,
                u.LockoutEnd))
            .ToListAsync();

        return Results.Ok(users);
    }

    private static async Task<IResult> GetUserById(
        Guid id,
        ApplicationDbContext context)
    {
        var user = await context.Users
            .AsNoTracking()
            .SingleOrDefaultAsync(u => u.Id == id);

        if (user is null)
        {
            return Results.NotFound(new { message = $"User with ID '{id}' was not found." });
        }

        string? personnelName = null;
        if (user.PersonnelId.HasValue)
        {
            var p = await context.Personnel
                .AsNoTracking()
                .SingleOrDefaultAsync(x => x.Id == user.PersonnelId.Value);
            if (p != null)
            {
                personnelName = $"{p.FirstName} {p.LastName} ({p.PersonalCode})";
            }
        }

        var details = new UserDetailsDto(
            user.Id,
            user.UserName,
            user.Email,
            user.EmailConfirmed,
            user.PersonnelId,
            personnelName,
            user.PhoneNumber,
            user.PhoneNumberConfirmed,
            user.TwoFactorEnabled,
            user.LockoutEnabled,
            user.LockoutEnd);

        return Results.Ok(details);
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

public record UserSummaryDto(
    Guid Id,
    string? UserName,
    string? Email,
    bool EmailConfirmed,
    int? PersonnelId,
    bool LockoutEnabled,
    DateTimeOffset? LockoutEnd);

public record UserDetailsDto(
    Guid Id,
    string? UserName,
    string? Email,
    bool EmailConfirmed,
    int? PersonnelId,
    string? PersonnelName,
    string? PhoneNumber,
    bool PhoneNumberConfirmed,
    bool TwoFactorEnabled,
    bool LockoutEnabled,
    DateTimeOffset? LockoutEnd);

public record RegisterRequest(string Email, string Password, int? PersonnelId = null);

public record LoginRequest(string Email, string Password);

public record AuthResponse(string Token, Guid UserId, int? PersonnelId);
