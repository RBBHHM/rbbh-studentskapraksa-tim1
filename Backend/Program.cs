using RBBH.ConnectedParties.IoC;
using RBBH.ConnectedParties.IoC.Extensions.Health;
using RBBH.ConnectedParties.IoC.Extensions.HTTPMetricsExtension;
using RBBH.ConnectedParties.IoC.Extensions.Security;
using RBBH.ConnectedParties.IoC.Extensions.Swagger;
using RBBH.ConnectedParties.IoC.Middleware;
using RBBH.ConnectedParties.IoC.Extensions.Authentication;
using RBBH.ConnectedParties.IoC.Extensions.Databases;
using RBBH.ConnectedParties.DL.Persistence;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using dotenv.net;
using RBBH.ConnectedParties.Helpers;

DotEnv.Load();

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.ConfigureKestrel(options => options.AddServerHeader = false);
builder.Services.AddApplicationForwardedHeaders(builder.Configuration, builder.Environment);
builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-CSRF-TOKEN";
    options.Cookie.SecurePolicy = builder.Environment.IsDevelopment() ? CookieSecurePolicy.SameAsRequest : CookieSecurePolicy.Always;
});

// Dependency Injection
builder.Services.AddServicesExtensions(builder.Configuration, builder.Environment, builder.Host);
builder.Services.AddHostedService<RBBH.ConnectedParties.Services.AutoLockHostedService>();
builder.Services.AddHostedService<RBBH.ConnectedParties.Services.NotificationDeliveryWorker>();

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

// Include authentication/authorization short-circuits without logging identity data.
app.UseMiddleware<RequestOutcomeLoggingMiddleware>();

app.UseAuthentication();
app.UseAuthorization();

app.UseMiddleware<RBBH.ConnectedParties.Middlewares.PeriodLockMiddleware>();

app.MapApplicationAuthentication();

app.MapControllers();

if (app.Environment.IsDevelopment() && app.Configuration.GetValue<bool>("Database:ApplyMigrations"))
{
    using var scope = app.Services.CreateScope();

    var db = scope.ServiceProvider
        .GetRequiredService<ConnectedPartiesDbContext>();

    if (db.Database.IsRelational())
        await db.Database.MigrateAsync();
    else
        app.Logger.LogWarning("Development migration request ignored for the non-relational database provider.");
}

app.Run();
