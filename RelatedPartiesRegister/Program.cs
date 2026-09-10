using RBBH.ConnectedParties.IoC;
using RBBH.ConnectedParties.IoC.Extensions.Health;
using RBBH.ConnectedParties.IoC.Extensions.HTTPMetricsExtension;
using RBBH.ConnectedParties.IoC.Extensions.Security;
using RBBH.ConnectedParties.IoC.Extensions.Swagger;
using RBBH.ConnectedParties.IoC.Middleware;
using RBBH.ConnectedParties.IoC.Extensions.Authentication;
using RBBH.ConnectedParties.IoC.Extensions.Databases;
using Microsoft.AspNetCore.HttpOverrides;
using dotenv.net;

DotEnv.Load();

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.ConfigureKestrel(options => options.AddServerHeader = false);
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor |
        ForwardedHeaders.XForwardedHost |
        ForwardedHeaders.XForwardedProto;
    options.ForwardLimit = 1;
    // OCP/IIS proxy addresses are dynamic and controlled by the hosting network.
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});

// Dependency Injection
builder.Services.AddServicesExtensions(builder.Configuration, builder.Environment, builder.Host);
builder.Services.AddHostedService<RBBH.ConnectedParties.Services.AutoLockHostedService>();

// Build app
var app = builder.Build();

app.UseForwardedHeaders();

var authWarning = app.Services.GetService<AuthenticationStartupWarning>();
if (authWarning is not null)
    app.Logger.LogWarning("{AuthenticationWarning}", authWarning.Message);

var databaseWarning = app.Services.GetService<DatabaseStartupWarning>();
if (databaseWarning is not null)
    app.Logger.LogWarning("{DatabaseWarning}", databaseWarning.Message);

// Enable Swagger page
if (!builder.Environment.IsProduction())
{
    app.UseSwaggerExtension();
}
else
{
    app.UseHsts();
}

// Add Correlation Id header
app.UseMiddleware<IncludeCorrelationIDMiddleware>();

// Configure the HTTP request pipeline.
app.UseExceptionHandler();

app.AddCSPConfig();

app.AddHTTPMetricsExtension();

app.MapCustomHealthChecks();

//#if (IsAPI)
// Use CORS for API projects
app.UseCors();
//#endif

app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<RBBH.ConnectedParties.Middlewares.PeriodLockMiddleware>();

app.MapGet("/authentication/login", (string? returnUrl) =>
    Results.Challenge(
        new Microsoft.AspNetCore.Authentication.AuthenticationProperties
        {
            RedirectUri = SafeLocalReturnUrl(returnUrl)
        },
        ["KeycloakOidc"]));
app.MapGet("/authentication/logout", () =>
    Results.SignOut(
        new Microsoft.AspNetCore.Authentication.AuthenticationProperties { RedirectUri = "/" },
        [Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationDefaults.AuthenticationScheme, "KeycloakOidc"]));
app.MapGet("/api/auth/profile", (HttpContext context) => Results.Ok(new
{
    userId = context.User.FindFirst("sub")?.Value
        ?? context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value,
    fullName = context.User.FindFirst("name")?.Value ?? context.User.Identity?.Name,
    username = context.User.FindFirst("preferred_username")?.Value,
    email = context.User.FindFirst("email")?.Value,
    roles = context.User.FindAll(System.Security.Claims.ClaimTypes.Role).Select(claim => claim.Value).Distinct(),
    permissions = context.User.FindAll("permission").Select(claim => claim.Value).Distinct()
})).RequireAuthorization();

app.MapControllers();

app.Run();

static string SafeLocalReturnUrl(string? returnUrl) =>
    !string.IsNullOrWhiteSpace(returnUrl) && returnUrl.StartsWith('/') && !returnUrl.StartsWith("//")
        ? returnUrl
        : "/app";
