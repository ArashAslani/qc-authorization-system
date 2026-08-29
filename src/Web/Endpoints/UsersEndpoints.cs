using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using qc_authorization.Application.Common.Interfaces;
using qc_authorization.Application.Organization.Queries.GetPersonnelWorkspaces;
using qc_authorization.Infrastructure.Data;
using qc_authorization.Infrastructure.Identity;
using qc_authorization.Web.Infrastructure;
using MediatR;

namespace qc_authorization.Web.Endpoints;

public class UsersEndpoints : IEndpointGroup
{
    public static string? RoutePrefix => "/api/users";

    public static void Map(RouteGroupBuilder group)
    {
        group.MapGet(GetUsers);
        group.MapGet(GetUserById, "{id:guid}");
        group.MapGet(GetMyWorkspaces, "me/workspaces").RequireAuthorization();
        group.MapPost(Register, "register");
        group.MapPost(Login, "login");
        group.MapPost(SwitchCompany, "me/switch-company").RequireAuthorization();
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

    private static async Task<IResult> GetMyWorkspaces(
        ICurrentUser currentUser,
        ISender sender)
    {
        if (!currentUser.IsAuthenticated || currentUser.PersonnelId is not Guid personnelId)
        {
            return Results.Unauthorized();
        }

        var workspaces = await sender.Send(new GetPersonnelWorkspacesQuery(personnelId));
        return Results.Ok(workspaces);
    }

    private static async Task<IResult> Register(
        RegisterRequest request,
        UserManager<ApplicationUser> userManager,
        IPersonnelIdentityBridge personnelIdentityBridge,
        JwtTokenService jwtTokenService,
        ApplicationDbContext context,
        ISender sender)
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

        string? nationalId = null;
        if (request.PersonnelId.HasValue)
        {
            await personnelIdentityBridge.LinkAsync(
                request.PersonnelId.Value,
                user.Id,
                CancellationToken.None);

            user = (await userManager.FindByIdAsync(user.Id.ToString()))!;
            nationalId = await context.Personnel
                .AsNoTracking()
                .Where(p => p.Id == request.PersonnelId.Value)
                .Select(p => p.NationalId)
                .SingleOrDefaultAsync();
        }

        Guid? activeCompanyId = null;
        PersonnelWorkspacesDto? workspaces = null;
        if (user.PersonnelId.HasValue)
        {
            workspaces = await sender.Send(new GetPersonnelWorkspacesQuery(user.PersonnelId.Value));
            activeCompanyId = workspaces.DefaultCompanyId;
        }

        var token = jwtTokenService.GenerateToken(user, activeCompanyId, nationalId);
        return Results.Ok(BuildAuthResponse(token, user, activeCompanyId, workspaces));
    }

    private static async Task<IResult> Login(
        LoginRequest request,
        UserManager<ApplicationUser> userManager,
        JwtTokenService jwtTokenService,
        ApplicationDbContext context,
        ISender sender)
    {
        ApplicationUser? user = null;
        string? nationalId = null;

        if (!string.IsNullOrWhiteSpace(request.NationalId))
        {
            nationalId = request.NationalId.Trim();
            var personnel = await context.Personnel
                .AsNoTracking()
                .SingleOrDefaultAsync(p => p.NationalId == nationalId);

            if (personnel?.IdentityUserId is Guid identityUserId)
            {
                user = await userManager.FindByIdAsync(identityUserId.ToString());
            }
        }
        else if (!string.IsNullOrWhiteSpace(request.Email))
        {
            user = await userManager.FindByEmailAsync(request.Email.Trim());
            if (user?.PersonnelId is Guid personnelId)
            {
                nationalId = await context.Personnel
                    .AsNoTracking()
                    .Where(p => p.Id == personnelId)
                    .Select(p => p.NationalId)
                    .SingleOrDefaultAsync();
            }
        }

        if (user is null || !await userManager.CheckPasswordAsync(user, request.Password))
        {
            throw new UnauthorizedAccessException("Invalid credentials.");
        }

        PersonnelWorkspacesDto? workspaces = null;
        Guid? activeCompanyId = null;
        if (user.PersonnelId.HasValue)
        {
            workspaces = await sender.Send(new GetPersonnelWorkspacesQuery(user.PersonnelId.Value));
            activeCompanyId = workspaces.DefaultCompanyId;
        }

        var token = jwtTokenService.GenerateToken(user, activeCompanyId, nationalId);
        return Results.Ok(BuildAuthResponse(token, user, activeCompanyId, workspaces));
    }

    private static async Task<IResult> SwitchCompany(
        SwitchCompanyRequest request,
        ICurrentUser currentUser,
        UserManager<ApplicationUser> userManager,
        JwtTokenService jwtTokenService,
        ApplicationDbContext context,
        ISender sender)
    {
        if (!currentUser.IsAuthenticated || currentUser.UserId is not Guid userId || currentUser.PersonnelId is not Guid personnelId)
        {
            return Results.Unauthorized();
        }

        var workspaces = await sender.Send(new GetPersonnelWorkspacesQuery(personnelId));
        var targetCompany = workspaces.Companies.SingleOrDefault(c => c.CompanyId == request.CompanyId);
        if (targetCompany is null)
        {
            return Results.Forbid();
        }

        var user = await userManager.FindByIdAsync(userId.ToString())
            ?? throw new UnauthorizedAccessException("User not found.");

        var nationalId = await context.Personnel
            .AsNoTracking()
            .Where(p => p.Id == personnelId)
            .Select(p => p.NationalId)
            .SingleOrDefaultAsync();

        var token = jwtTokenService.GenerateToken(user, request.CompanyId, nationalId);
        return Results.Ok(BuildAuthResponse(token, user, request.CompanyId, workspaces));
    }

    private static AuthResponse BuildAuthResponse(
        string token,
        ApplicationUser user,
        Guid? activeCompanyId,
        PersonnelWorkspacesDto? workspaces)
    {
        var activePositions = workspaces?.Companies
            .FirstOrDefault(c => c.CompanyId == activeCompanyId)
            ?.Positions ?? [];

        return new AuthResponse(
            token,
            user.Id,
            user.PersonnelId,
            activeCompanyId,
            workspaces?.Companies.Select(c => c.CompanyId).ToList() ?? [],
            activePositions.Select(p => new ActivePositionDto(p.PositionId, p.PositionCode, p.PositionTitle, p.IsPrimary)).ToList());
    }
}

public record UserSummaryDto(
    Guid Id,
    string? UserName,
    string? Email,
    bool EmailConfirmed,
    Guid? PersonnelId,
    bool LockoutEnabled,
    DateTimeOffset? LockoutEnd);

public record UserDetailsDto(
    Guid Id,
    string? UserName,
    string? Email,
    bool EmailConfirmed,
    Guid? PersonnelId,
    string? PersonnelName,
    string? PhoneNumber,
    bool PhoneNumberConfirmed,
    bool TwoFactorEnabled,
    bool LockoutEnabled,
    DateTimeOffset? LockoutEnd);

public record RegisterRequest(string Email, string Password, Guid? PersonnelId = null);

public record LoginRequest(string Password, string? Email = null, string? NationalId = null);

public record SwitchCompanyRequest(Guid CompanyId);

public record ActivePositionDto(Guid PositionId, string PositionCode, string PositionTitle, bool IsPrimary);

public record AuthResponse(
    string Token,
    Guid UserId,
    Guid? PersonnelId,
    Guid? ActiveCompanyId,
    IReadOnlyList<Guid> AvailableCompanies,
    IReadOnlyList<ActivePositionDto> ActiveCompanyPositions);
