using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using RBBH.ConnectedParties.API.Controllers;
using RBBH.ConnectedParties.BL.ServiceInterfaces;
using RBBH.ConnectedParties.BL.Services;
using RBBH.ConnectedParties.DL.Entities.Role;
using RBBH.ConnectedParties.DL.Entities.Users;
using RBBH.ConnectedParties.DL.Persistence;
using RBBH.ConnectedParties.Helpers.Constants;
using RBBH.ConnectedParties.IoC.Extensions.Authentication;
using RBBH.ConnectedParties.IoC.Extensions.ExceptionHandling;
using RBBH.ConnectedParties.Middlewares;

namespace IntegrationTests.Infrastructure;

/// <summary>Real API pipeline; only authentication, mail and disposable storage are fixtures.</summary>
public abstract class AuthenticatedApiFixture : IAsyncLifetime
{
    protected WebApplication App = null!;
    protected HttpClient Client = null!;
    protected virtual bool UseSql => false;
    private readonly string _database = "RBBH_FINALIZATION_" + Guid.NewGuid().ToString("N");
    private string? _connection;
    protected virtual void ConfigureDatabase(DbContextOptionsBuilder options) { }
    protected virtual void ConfigureServices(IServiceCollection services) { }
    protected virtual void ConfigureEndpoints(WebApplication app) { }

    public async Task InitializeAsync()
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        { EnvironmentName = "Testing", ApplicationName = typeof(VerificationController).Assembly.FullName });
        builder.WebHost.UseTestServer(); builder.Logging.ClearProviders();
        builder.Configuration["Email:AdminEmail"] = "admin@example.test";
        builder.Services.AddControllers().AddApplicationPart(typeof(VerificationController).Assembly);
        builder.Services.AddExceptionHandlingExtension(builder.Environment);
        builder.Services.AddHttpContextAccessor();
        builder.Services.AddDbContext<ConnectedPartiesDbContext>(options =>
        {
            if (UseSql && !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("RBBH_TEST_SQLSERVER")))
            {
                _connection = new SqlConnectionStringBuilder(Environment.GetEnvironmentVariable("RBBH_TEST_SQLSERVER"))
                { InitialCatalog = _database }.ConnectionString;
                options.UseSqlServer(_connection, sql => sql.EnableRetryOnFailure());
            }
            else options.UseInMemoryDatabase(_database);
            ConfigureDatabase(options);
        });
        builder.Services.AddAuthentication("IdentityFixture")
            .AddScheme<AuthenticationSchemeOptions, IdentityFixture>("IdentityFixture", _ => { });
        builder.Services.AddScoped<IClaimsTransformation, RoleClaimsTransformation>();
        builder.Services.AddApplicationAuthorization();
        builder.Services.AddScoped<ILegalEntityService, LegalEntityService>();
        builder.Services.AddScoped<IRelatedPersonService, RelatedPersonService>();
        builder.Services.AddScoped<IAuditService, AuditService>();
        builder.Services.AddScoped<IPeriodLockRepository, PeriodLockRepository>();
        builder.Services.AddScoped<PeriodWorkflow>();
        builder.Services.AddScoped<IUnlockRequestRepository, UnlockRequestRepository>();
        builder.Services.AddScoped<IPeriodLockService, PeriodLockService>();
        builder.Services.AddOptions<EmailSettings>();
        builder.Services.AddSingleton(Mock.Of<IEmailService>());
        ConfigureServices(builder.Services);
        App = builder.Build(); App.UseExceptionHandler(); App.UseAuthentication(); App.UseAuthorization();
        App.UseMiddleware<PeriodLockMiddleware>(); App.MapControllers(); ConfigureEndpoints(App);
        await App.StartAsync(); Client = NewClient("creator");
        await InDatabase(async db =>
        {
            if (_connection is not null) await db.Database.MigrateAsync();
            await DevelopmentDataSeeder.EnsureApplicationRolesAsync(db);
            foreach (var subject in new[] { "creator", "reviewer", "admin2" })
            {
                var user = new AppUser { Username = subject, Email = subject + "@example.test", KeycloakId = subject, KeycloakSubjectConfirmed = true };
                db.AppUsers.Add(user);
                db.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = (await db.Roles.SingleAsync(role => role.Name == ApplicationAccessRoles.Administrator)).Id });
            }
            db.PeriodLocks.Add(new() { Year = DateTime.UtcNow.Year, Month = DateTime.UtcNow.Month, IsLocked = false, CreatedBy = "fixture" });
            await db.SaveChangesAsync();
        });
    }

    protected HttpClient NewClient(string subject)
    {
        var client = App.GetTestClient(); client.DefaultRequestHeaders.Add("X-Subject", subject); return client;
    }

    protected async Task InDatabase(Func<ConnectedPartiesDbContext, Task> action)
    {
        await using var scope = App.Services.CreateAsyncScope();
        await action(scope.ServiceProvider.GetRequiredService<ConnectedPartiesDbContext>());
    }

    public async Task DisposeAsync()
    {
        Client.Dispose(); await App.DisposeAsync();
        if (_connection is null) return;
        var options = new DbContextOptionsBuilder<ConnectedPartiesDbContext>().UseSqlServer(_connection).Options;
        await using var db = new ConnectedPartiesDbContext(options);
        await db.Database.EnsureDeletedAsync(); // Only this fixture's GUID-named disposable database.
    }

    private sealed class IdentityFixture(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder)
        : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
    {
        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (Request.Headers.ContainsKey("X-Fixture-Anonymous")) return Task.FromResult(AuthenticateResult.NoResult());
            var subject = Request.Headers["X-Subject"].FirstOrDefault() ?? "creator";
            if (Request.Headers.ContainsKey("X-Fixture-Empty-Subject")) subject = "";
            if (subject == "anonymous") return Task.FromResult(AuthenticateResult.NoResult());
            Claim[] claims = [new("sub", subject), new(ClaimTypes.Email, Request.Headers["X-Email"].FirstOrDefault() ?? subject + "@example.test"),
                new("preferred_username", subject), new("email_verified", Request.Headers["X-Email-Verified"].FirstOrDefault() ?? "true")];
            return Task.FromResult(AuthenticateResult.Success(new(new ClaimsPrincipal(new ClaimsIdentity(claims, Scheme.Name)), Scheme.Name)));
        }
    }
}
