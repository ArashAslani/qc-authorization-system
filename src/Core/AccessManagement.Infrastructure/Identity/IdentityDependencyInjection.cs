using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
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
    public const string DevelopmentSigningKey = "qc-authorization-dev-signing-key-min-32-chars";

    public static void AddIdentityServices(this IHostApplicationBuilder builder)
    {
        builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.SectionName));

        builder.Services
            .AddIdentityCore<ApplicationUser>(options =>
            {
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = true;
                options.Password.RequiredLength = 12;
                options.User.RequireUniqueEmail = true;
                options.Lockout.AllowedForNewUsers = true;
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
                options.SignIn.RequireConfirmedEmail = true;
            })
            .AddRoles<ApplicationRole>()
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddSignInManager()
            .AddDefaultTokenProviders();

        builder.Services.AddScoped<IPersonnelIdentityBridge, PersonnelIdentityBridge>();
        builder.Services.AddScoped<JwtTokenService>();
        builder.Services.AddScoped<ITokenRevocationStore, TokenRevocationStore>();
        builder.Services.AddSingleton<IEmailConfirmationService, DevelopmentEmailConfirmationService>();
        builder.Services.AddSingleton<ITwoFactorChallengeService, DevelopmentTwoFactorChallengeService>();

        var jwtKey = builder.Configuration["Jwt:Key"];
        if (string.IsNullOrWhiteSpace(jwtKey))
        {
            throw new InvalidOperationException("Jwt:Key must be configured via Jwt__Key or Jwt:Key.");
        }

        if (builder.Environment.IsProduction()
            && jwtKey == DevelopmentSigningKey)
        {
            throw new InvalidOperationException("Jwt:Key must not use the Development signing key in Production.");
        }

        builder.Services.PostConfigure<JwtOptions>(options =>
        {
            if (string.IsNullOrWhiteSpace(options.Key))
            {
                options.Key = jwtKey;
            }
        });

        var jwtOptions = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
            ?? new JwtOptions();

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
                options.Events = new JwtBearerEvents
                {
                    OnTokenValidated = async context =>
                    {
                        var tokenUse = context.Principal?.FindFirstValue(JwtTokenService.TokenUseClaim);
                        if (tokenUse == JwtTokenService.TokenUseTwoFactor)
                        {
                            context.Fail("Two-factor challenge tokens cannot access the API.");
                            return;
                        }

                        var userManager = context.HttpContext.RequestServices.GetRequiredService<UserManager<ApplicationUser>>();
                        var revocation = context.HttpContext.RequestServices.GetRequiredService<ITokenRevocationStore>();
                        var userId = context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
                        if (!Guid.TryParse(userId, out var id))
                        {
                            context.Fail("Invalid token subject.");
                            return;
                        }

                        var user = await userManager.FindByIdAsync(id.ToString());
                        if (user is null)
                        {
                            context.Fail("User not found.");
                            return;
                        }

                        var stamp = context.Principal?.FindFirstValue(JwtTokenService.SecurityStampClaim);
                        if (!string.IsNullOrEmpty(user.SecurityStamp)
                            && !string.Equals(user.SecurityStamp, stamp, StringComparison.Ordinal))
                        {
                            context.Fail("Token has been revoked.");
                            return;
                        }

                        var jti = context.Principal?.FindFirstValue(JwtRegisteredClaimNames.Jti);
                        if (!string.IsNullOrEmpty(jti) && await revocation.IsRevokedAsync(jti))
                        {
                            context.Fail("Token has been revoked.");
                        }
                    },
                };
            });
    }
}
