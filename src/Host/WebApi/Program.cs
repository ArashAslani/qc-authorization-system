using AccessManagement.Infrastructure.Data;
using Qc.AccessPlugin;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.AddAccessManagementCore();
builder.Services.AddQcAccessPlugin();
builder.AddInfrastructureServices();
builder.AddWebServices();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    await app.InitialiseDatabaseAsync();
    app.MapOpenApi();
}
else
{
    app.UseHsts();
}

app.UseHttpsRedirection();
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

app.UseAuthentication();
app.UseAuthorization();

app.UseExceptionHandler(options => { });
app.MapEndpoints(typeof(Program).Assembly);

app.Run();
