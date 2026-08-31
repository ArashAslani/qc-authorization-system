using AccessManagement.Application.Common.Interfaces;
using AccessManagement.Infrastructure.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace AccessManagement.Infrastructure.Identity;

public static class CurrentUserState
{
    public const string HttpContextItemKey = "AccessManagement.HydratedCurrentUser";
}

public sealed record HydratedCurrentUser(Guid UserId, Guid? PersonnelId, Guid? ActiveCompanyId);

public sealed class CurrentUserHydrationMiddleware
{
    private readonly RequestDelegate _next;

    public CurrentUserHydrationMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context, ApplicationDbContext db)
    {
        var id = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (Guid.TryParse(id, out var userId))
        {
            var now = DateTimeOffset.UtcNow;
            var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);
            Guid? personnelId = user?.PersonnelId
                ?? await db.Personnel.AsNoTracking()
                    .Where(p => p.IdentityUserId == userId)
                    .Select(p => (Guid?)p.Id)
                    .FirstOrDefaultAsync();

            Guid? activeCompanyId = user?.ActiveCompanyId;
            if (activeCompanyId is Guid company && personnelId is Guid pid)
            {
                var stillValid = await db.PositionAssignments.AsNoTracking().AnyAsync(a =>
                    a.PersonnelId == pid
                    && a.Position.CompanyUnitId == company
                    && a.ValidFrom <= now
                    && (a.ValidTo == null || now <= a.ValidTo));
                if (!stillValid)
                {
                    activeCompanyId = null;
                }
            }
            else
            {
                activeCompanyId = null;
            }

            context.Items[CurrentUserState.HttpContextItemKey] = new HydratedCurrentUser(
                userId, personnelId, activeCompanyId);
        }

        await _next(context);
    }
}

public sealed class CurrentUserService : ICurrentUser
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid? UserId
    {
        get
        {
            var id = _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.TryParse(id, out var guid) ? guid : null;
        }
    }

    public bool IsAuthenticated =>
        _httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated == true;

    public Guid? PersonnelId => Hydrated?.PersonnelId;

    public Guid? ActiveCompanyId => Hydrated?.ActiveCompanyId;

    private HydratedCurrentUser? Hydrated =>
        _httpContextAccessor.HttpContext?.Items.TryGetValue(CurrentUserState.HttpContextItemKey, out var value) == true
            ? value as HydratedCurrentUser
            : null;
}
