using System.Threading.RateLimiting;
using AccessManagement.Infrastructure.Data;
using AccessManagement.Infrastructure.Identity;
using AccessManagement.WebApi.Endpoints;
using AccessManagement.WebApi.Infrastructure;
using Microsoft.AspNetCore.RateLimiting;
using Qc.AccessPlugin;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.AddAccessManagementCore();
builder.Services.AddQcAccessPlugin();
builder.AddInfrastructureServices();
builder.AddWebServices();

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    var permitLimit = builder.Environment.IsEnvironment("Testing") ? 10_000 : 10;
    options.AddPolicy(UsersEndpoints.AuthRateLimitPolicy, httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = permitLimit,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
            }));
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    await app.InitialiseDatabaseAsync();
    app.MapOpenApi();
}
else if (!app.Environment.IsEnvironment("Testing"))
{
    app.UseHsts();
}

if (!app.Environment.IsEnvironment("Testing"))
{
    app.UseHttpsRedirection();
}

app.Use(async (context, next) =>
{
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["X-Frame-Options"] = "DENY";
    context.Response.Headers["Referrer-Policy"] = "no-referrer";
    context.Response.Headers["Content-Security-Policy"] = "default-src 'none'; frame-ancestors 'none'";
    await next();
});

var allowedOrigins = app.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
app.UseCors(policy =>
{
    if (allowedOrigins.Length > 0)
    {
        policy.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod();
    }
    else
    {
        policy.SetIsOriginAllowed(_ => false);
    }
});

app.UseRateLimiter();
app.UseAuthentication();
app.UseMiddleware<CurrentUserHydrationMiddleware>();
app.UseAuthorization();

app.UseExceptionHandler(options => { });
app.MapEndpoints(typeof(Program).Assembly);

app.Run();

public partial class Program { }
