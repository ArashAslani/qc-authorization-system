using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using AccessManagement.Application.Common.Interfaces;
using AccessManagement.Infrastructure.Data;
using AccessManagement.Infrastructure.Identity;

namespace Microsoft.Extensions.DependencyInjection;

public static class IdentityDependencyInjection
{
    public static void AddIdentityServices(this IHostApplicationBuilder builder)
    {
        builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.SectionName));

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

        builder.Services.AddScoped<IPersonnelIdentityBridge, PersonnelIdentityBridge>();
        builder.Services.AddScoped<JwtTokenService>();

        var jwtOptions = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
            ?? new JwtOptions();

        var jwtKey = builder.Configuration["Jwt:Key"];
        if (string.IsNullOrWhiteSpace(jwtKey))
        {
            if (builder.Environment.IsDevelopment())
            {
                jwtKey = "qc-authorization-dev-signing-key-min-32-chars";
            }
            else
            {
                throw new InvalidOperationException("Jwt:Key must be configured via Jwt__Key or Jwt:Key.");
            }
        }

        builder.Services.PostConfigure<JwtOptions>(options =>
        {
            if (string.IsNullOrWhiteSpace(options.Key))
            {
                options.Key = jwtKey;
            }
        });

        builder.Services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtOptions.Issuer,
                    ValidAudience = jwtOptions.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
                    ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
                    ClockSkew = TimeSpan.FromMinutes(1),
                };
            });
    }
}
