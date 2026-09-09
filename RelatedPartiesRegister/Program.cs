using RBBH.ConnectedParties.IoC;
using RBBH.ConnectedParties.IoC.Extensions.Health;
using RBBH.ConnectedParties.IoC.Extensions.HTTPMetricsExtension;
using RBBH.ConnectedParties.IoC.Extensions.Security;
using RBBH.ConnectedParties.IoC.Extensions.Swagger;
using RBBH.ConnectedParties.IoC.Middleware;
using RBBH.ConnectedParties.IoC.Extensions.Authentication;
using RBBH.ConnectedParties.IoC.Extensions.Databases;
using RBBH.ConnectedParties.DL.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.HttpOverrides;
using RBBH.ConnectedParties.BL.Services;
using dotenv.net;

DotEnv.Load();

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.ConfigureKestrel(options => options.AddServerHeader = false);
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
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

await using (var scope = app.Services.CreateAsyncScope())
{
    var database = scope.ServiceProvider.GetRequiredService<ConnectedPartiesDbContext>();
    if (database.Database.IsRelational())
    {
        var applyMigrations = app.Environment.IsDevelopment()
            || builder.Configuration.GetValue<bool>("Database:ApplyMigrations");
        var pendingMigrations = (await database.Database.GetPendingMigrationsAsync()).ToArray();

        if (applyMigrations && pendingMigrations.Length > 0)
            await database.Database.MigrateAsync();
        else if (!applyMigrations && pendingMigrations.Length > 0)
            throw new InvalidOperationException(
                $"Baza nije usklađena s aplikacijom. Nedostaju migracije: {string.Join(", ", pendingMigrations)}. " +
                "Na UAT/produkciji primijenite odobreni idempotentni SQL kroz centralni DB repozitorij; " +
                "aplikacija namjerno ne izvršava DDL automatski.");
    }
    else
        await database.Database.EnsureCreatedAsync();

    if (app.Environment.IsDevelopment() && !database.Database.IsRelational())
    {
        await DevelopmentDataSeeder.SeedAsync(database);
        var emailLog = scope.ServiceProvider.GetRequiredService<EmailLogStore>();
        if (emailLog.GetAll().Count == 0)
        {
            emailLog.Add(new EmailLogEntry { To = "admin@localhost", Subject = "Zahtjev za otključavanje perioda", HtmlBody = "<p>Korisnik je zatražio otključavanje trenutnog perioda radi korekcije podataka.</p>", Audience = "admin", SentAt = DateTime.UtcNow.AddMinutes(-45) });
            emailLog.Add(new EmailLogEntry { To = "verifier@localhost", Subject = "Period je uspješno otključan", HtmlBody = "<p>Period je otključan i unos podataka je ponovo dozvoljen.</p>", Audience = "user", SentAt = DateTime.UtcNow.AddHours(-2) });
            emailLog.Add(new EmailLogEntry { To = "hr@localhost", Subject = "Novo povezano fizičko lice", HtmlBody = "<p>U registar je dodano novo povezano fizičko lice.</p>", Audience = "hr", SentAt = DateTime.UtcNow.AddDays(-1) });
        }
    }
    else
        await DevelopmentDataSeeder.EnsureApplicationRolesAsync(database);
}

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
