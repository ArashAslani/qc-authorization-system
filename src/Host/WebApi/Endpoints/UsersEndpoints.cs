using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AccessManagement.Application.Authorization.Services;
using AccessManagement.Application.Common.Interfaces;
using AccessManagement.Application.Common.Models;
using AccessManagement.Application.Organization.Queries.GetPersonnelWorkspaces;
using AccessManagement.Infrastructure.Data;
using AccessManagement.Infrastructure.Identity;
using AccessManagement.WebApi.Infrastructure;
using MediatR;

namespace AccessManagement.WebApi.Endpoints;

public class UsersEndpoints : IEndpointGroup
{
    public const string AuthRateLimitPolicy = "auth";

    public static string? RoutePrefix => "/api/users";

    public static void Map(RouteGroupBuilder group)
    {
        group.MapGet(GetUsers);
        group.MapGet(GetUserById, "{id:guid}");
        group.MapGet(GetMyWorkspaces, "me/workspaces");
        group.MapPost(Register, "register").AllowAnonymous().RequireRateLimiting(AuthRateLimitPolicy);
        group.MapPost(ConfirmEmail, "confirm-email").AllowAnonymous().RequireRateLimiting(AuthRateLimitPolicy);
        group.MapPost(Login, "login").AllowAnonymous().RequireRateLimiting(AuthRateLimitPolicy);
        group.MapPost(LoginTwoFactor, "login/2fa").AllowAnonymous().RequireRateLimiting(AuthRateLimitPolicy);
        group.MapPost(Logout, "logout");
        group.MapPost(SwitchCompany, "me/switch-company");
        group.MapPost(SetTwoFactor, "me/two-factor");
    }

    private static async Task<IResult> GetUsers(
        [FromQuery] string? searchTerm,
        [FromQuery] bool? hasPersonnel,
        [FromQuery] int pageNumber,
        [FromQuery] int pageSize,
        ApplicationDbContext context,
        ICompanyVisibilityService visibility)
    {
        var vis = await visibility.ResolveAsync();
        var query = context.Users.AsNoTracking().AsQueryable();

        if (!vis.IsAdmin)
        {
            var userIds = vis.UserIds.ToList();
            query = query.Where(u => userIds.Contains(u.Id));
        }

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.Trim();
            if (term.Length > 100)
            {
                term = term[..100];
            }

            term = term.ToLower();
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

        var (page, size) = PaginatedList<UserSummaryDto>.Normalize(
            pageNumber == 0 ? 1 : pageNumber,
            pageSize == 0 ? PaginatedList<UserSummaryDto>.DefaultPageSize : pageSize);
        var totalCount = await query.CountAsync();
        var users = await query
            .OrderBy(u => u.Email)
            .Skip((page - 1) * size)
            .Take(size)
            .Select(u => vis.IsAdmin
                ? new UserSummaryDto(u.Id, u.UserName, u.Email, u.EmailConfirmed, u.PersonnelId, u.LockoutEnabled, u.LockoutEnd)
                : new UserSummaryDto(u.Id, u.UserName, null, false, u.PersonnelId, false, null))
            .ToListAsync();

        return Results.Ok(new PaginatedList<UserSummaryDto>(users, totalCount, page, size));
    }

    private static async Task<IResult> GetUserById(
        Guid id,
        ApplicationDbContext context,
        ICurrentUser currentUser,
        ICompanyVisibilityService visibility)
    {
        var vis = await visibility.ResolveAsync();
        if (!vis.IsAdmin && currentUser.UserId != id && !vis.UserIds.Contains(id))
        {
            return Results.NotFound(new { message = $"User with ID '{id}' was not found." });
        }
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
                personnelName = $"{p.FirstName} {p.LastName} ({p.PersonnelCode})";
            }
        }

        var isSelfOrAdmin = vis.IsAdmin || currentUser.UserId == id;
        var details = new UserDetailsDto(
            user.Id,
            user.UserName,
            isSelfOrAdmin ? user.Email : null,
            isSelfOrAdmin && vis.IsAdmin ? user.EmailConfirmed : false,
            user.PersonnelId,
            personnelName,
            isSelfOrAdmin ? user.PhoneNumber : null,
            isSelfOrAdmin && vis.IsAdmin && user.PhoneNumberConfirmed,
            isSelfOrAdmin ? user.TwoFactorEnabled : false,
            vis.IsAdmin && user.LockoutEnabled,
            vis.IsAdmin ? user.LockoutEnd : null);

        return Results.Ok(details);
    }

    private static async Task<IResult> GetMyWorkspaces(
        ICurrentUser currentUser,
        ISender sender)
    {
        if (!currentUser.IsAuthenticated)
        {
            return Results.Unauthorized();
        }

        if (currentUser.PersonnelId is not Guid personnelId)
        {
            return Results.Problem(
                statusCode: StatusCodes.Status403Forbidden,
                title: "Personnel link required",
                detail: "Multi-company workspace is only available for users linked to a personnel record.");
        }

        var workspaces = await sender.Send(new GetPersonnelWorkspacesQuery(personnelId));
        return Results.Ok(workspaces);
    }

    private static async Task<IResult> Register(
        RegisterRequest request,
        UserManager<ApplicationUser> userManager,
        IEmailConfirmationService emailConfirmation,
        IHostEnvironment environment)
    {
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = request.Email.Trim(),
            Email = request.Email.Trim(),
            EmailConfirmed = false,
            LockoutEnabled = true,
        };

        var createResult = await userManager.CreateAsync(user, request.Password);
        if (!createResult.Succeeded)
        {
            return Results.ValidationProblem(createResult.Errors.ToDictionary(
                e => e.Code,
                e => new[] { e.Description }));
        }

        var confirmationToken = await emailConfirmation.CreateTokenAsync(user.Id, user.Email!);
        object payload = environment.IsDevelopment() || environment.IsEnvironment("Testing")
            ? new RegisterResponse(user.Id, confirmationToken)
            : new RegisterResponse(user.Id, null);
        return Results.Ok(payload);
    }

    private static async Task<IResult> ConfirmEmail(
        ConfirmEmailRequest request,
        UserManager<ApplicationUser> userManager,
        IEmailConfirmationService emailConfirmation)
    {
        var user = await userManager.FindByIdAsync(request.UserId.ToString());
        if (user is null)
        {
            throw new UnauthorizedAccessException("Invalid credentials.");
        }

        await emailConfirmation.ConfirmAsync(request.UserId, request.Token);
        user.EmailConfirmed = true;
        await userManager.UpdateAsync(user);
        return Results.NoContent();
    }

    private static async Task<IResult> Login(
        LoginRequest request,
        UserManager<ApplicationUser> userManager,
        JwtTokenService jwtTokenService,
        ApplicationDbContext context,
        ISender sender,
        ITwoFactorChallengeService twoFactor,
        IHostEnvironment environment)
    {
        var user = await FindUserForLoginAsync(request, userManager, context);

        if (user is null || !user.EmailConfirmed)
        {
            if (user is not null)
            {
                await userManager.AccessFailedAsync(user);
            }

            throw new UnauthorizedAccessException("Invalid credentials.");
        }

        if (await userManager.IsLockedOutAsync(user))
        {
            throw new UnauthorizedAccessException("Invalid credentials.");
        }

        if (!await userManager.CheckPasswordAsync(user, request.Password))
        {
            await userManager.AccessFailedAsync(user);
            throw new UnauthorizedAccessException("Invalid credentials.");
        }

        await userManager.ResetAccessFailedCountAsync(user);

        if (user.TwoFactorEnabled)
        {
            var code = await twoFactor.CreateChallengeCodeAsync(user.Id);
            var challenge = jwtTokenService.GenerateTwoFactorToken(user);
            return Results.Ok(new TwoFactorChallengeResponse(
                true,
                challenge,
                environment.IsDevelopment() || environment.IsEnvironment("Testing") ? code : null));
        }

        return Results.Ok(await IssueAccessAsync(user, userManager, jwtTokenService, sender));
    }

    private static async Task<IResult> LoginTwoFactor(
        LoginTwoFactorRequest request,
        UserManager<ApplicationUser> userManager,
        JwtTokenService jwtTokenService,
        ISender sender,
        ITwoFactorChallengeService twoFactor)
    {
        if (!jwtTokenService.TryReadToken(request.TwoFactorToken, out var jwt)
            || jwt.Claims.FirstOrDefault(c => c.Type == JwtTokenService.TokenUseClaim)?.Value != JwtTokenService.TokenUseTwoFactor)
        {
            throw new UnauthorizedAccessException("Invalid credentials.");
        }

        var sub = jwt.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value
            ?? jwt.Subject;
        if (!Guid.TryParse(sub, out var userId))
        {
            throw new UnauthorizedAccessException("Invalid credentials.");
        }

        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null || !user.TwoFactorEnabled)
        {
            throw new UnauthorizedAccessException("Invalid credentials.");
        }

        if (!await twoFactor.VerifyAsync(userId, request.Code))
        {
            throw new UnauthorizedAccessException("Invalid credentials.");
        }

        return Results.Ok(await IssueAccessAsync(user, userManager, jwtTokenService, sender));
    }

    private static async Task<IResult> Logout(
        HttpContext httpContext,
        ICurrentUser currentUser,
        ITokenRevocationStore revocation)
    {
        if (currentUser.UserId is not Guid userId)
        {
            return Results.Unauthorized();
        }

        var jti = httpContext.User.FindFirstValue(JwtRegisteredClaimNames.Jti);
        if (!string.IsNullOrEmpty(jti))
        {
            await revocation.RevokeAsync(jti, userId, DateTimeOffset.UtcNow.AddHours(2));
        }

        return Results.NoContent();
    }

    private static async Task<IResult> SwitchCompany(
        SwitchCompanyRequest request,
        HttpContext httpContext,
        ICurrentUser currentUser,
        UserManager<ApplicationUser> userManager,
        JwtTokenService jwtTokenService,
        ISender sender,
        ITokenRevocationStore revocation)
    {
        if (!currentUser.IsAuthenticated || currentUser.UserId is not Guid userId)
        {
            return Results.Unauthorized();
        }

        if (currentUser.PersonnelId is not Guid personnelId)
        {
            return Results.Problem(
                statusCode: StatusCodes.Status403Forbidden,
                title: "Personnel link required",
                detail: "Company switch is only available for users linked to a personnel record.");
        }

        var workspaces = await sender.Send(new GetPersonnelWorkspacesQuery(personnelId));
        var targetCompany = workspaces.Companies.SingleOrDefault(c => c.CompanyId == request.CompanyId);
        if (targetCompany is null)
        {
            return Results.Forbid();
        }

        var user = await userManager.FindByIdAsync(userId.ToString())
            ?? throw new UnauthorizedAccessException("User not found.");

        var previousJti = httpContext.User.FindFirstValue(JwtRegisteredClaimNames.Jti);
        if (!string.IsNullOrEmpty(previousJti))
        {
            await revocation.RevokeAsync(previousJti, userId, DateTimeOffset.UtcNow.AddHours(2));
        }

        user.ActiveCompanyId = request.CompanyId;
        await userManager.UpdateAsync(user);

        var token = jwtTokenService.GenerateToken(user, request.CompanyId);
        return Results.Ok(BuildAuthResponse(token, user, request.CompanyId, workspaces));
    }

    private static async Task<IResult> SetTwoFactor(
        SetTwoFactorRequest request,
        ICurrentUser currentUser,
        UserManager<ApplicationUser> userManager)
    {
        if (currentUser.UserId is not Guid userId)
        {
            return Results.Unauthorized();
        }

        var user = await userManager.FindByIdAsync(userId.ToString())
            ?? throw new UnauthorizedAccessException("User not found.");

        // TODO: replace with TOTP authenticator enrollment.
        user.TwoFactorEnabled = request.Enabled;
        await userManager.UpdateAsync(user);
        return Results.NoContent();
    }

    private static async Task<ApplicationUser?> FindUserForLoginAsync(
        LoginRequest request,
        UserManager<ApplicationUser> userManager,
        ApplicationDbContext context)
    {
        if (!string.IsNullOrWhiteSpace(request.NationalId))
        {
            var personnel = await context.Personnel
                .AsNoTracking()
                .SingleOrDefaultAsync(p => p.NationalId == request.NationalId.Trim());

            if (personnel?.IdentityUserId is Guid identityUserId)
            {
                return await userManager.FindByIdAsync(identityUserId.ToString());
            }

            return null;
        }

        if (!string.IsNullOrWhiteSpace(request.Email))
        {
            return await userManager.FindByEmailAsync(request.Email.Trim());
        }

        return null;
    }

    private static async Task<AuthResponse> IssueAccessAsync(
        ApplicationUser user,
        UserManager<ApplicationUser> userManager,
        JwtTokenService jwtTokenService,
        ISender sender)
    {
        PersonnelWorkspacesDto? workspaces = null;
        Guid? activeCompanyId = user.ActiveCompanyId;
        if (user.PersonnelId.HasValue)
        {
            workspaces = await sender.Send(new GetPersonnelWorkspacesQuery(user.PersonnelId.Value));
            if (activeCompanyId is null || workspaces.Companies.All(c => c.CompanyId != activeCompanyId))
            {
                activeCompanyId = workspaces.DefaultCompanyId;
            }

            user.ActiveCompanyId = activeCompanyId;
            await userManager.UpdateAsync(user);
        }

        var token = jwtTokenService.GenerateToken(user, activeCompanyId);
        return BuildAuthResponse(token, user, activeCompanyId, workspaces);
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

public record RegisterRequest(string Email, string Password);

public record RegisterResponse(Guid UserId, string? EmailConfirmationToken);

public record ConfirmEmailRequest(Guid UserId, string Token);

public record LoginRequest(string Password, string? Email = null, string? NationalId = null);

public record LoginTwoFactorRequest(string TwoFactorToken, string Code);

public record TwoFactorChallengeResponse(bool RequiresTwoFactor, string TwoFactorToken, string? DebugCode);

public record SetTwoFactorRequest(bool Enabled);

public record SwitchCompanyRequest(Guid CompanyId);

public record ActivePositionDto(Guid PositionId, string PositionCode, string PositionTitle, bool IsPrimary);

public record AuthResponse(
    string Token,
    Guid UserId,
    Guid? PersonnelId,
    Guid? ActiveCompanyId,
    IReadOnlyList<Guid> AvailableCompanies,
    IReadOnlyList<ActivePositionDto> ActiveCompanyPositions);
