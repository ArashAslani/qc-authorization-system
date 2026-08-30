using AccessManagement.Application.Common.Interfaces;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace AccessManagement.Infrastructure.Identity;

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

    public Guid? PersonnelId
    {
        get
        {
            var value = _httpContextAccessor.HttpContext?.User?.FindFirstValue("personnel_id");
            return Guid.TryParse(value, out var id) ? id : null;
        }
    }

    public Guid? ActiveCompanyId
    {
        get
        {
            var value = _httpContextAccessor.HttpContext?.User?.FindFirstValue("active_company_id");
            return Guid.TryParse(value, out var id) ? id : null;
        }
    }
}
