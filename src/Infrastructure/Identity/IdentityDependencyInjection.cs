using Microsoft.AspNetCore.Identity;
using qc_authorization.Infrastructure.Data;
using qc_authorization.Infrastructure.Identity;
using Microsoft.Extensions.Hosting;

namespace Microsoft.Extensions.DependencyInjection;

public static class IdentityDependencyInjection
{
    public static void AddIdentityServices(this IHostApplicationBuilder builder)
    {
        builder.Services
            .AddIdentityCore<ApplicationUser>(options =>
            {
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = false;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequiredLength = 8;
                options.User.RequireUniqueEmail = true;
            })
            .AddRoles<ApplicationRole>()
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddSignInManager()
            .AddDefaultTokenProviders();
    }
}
