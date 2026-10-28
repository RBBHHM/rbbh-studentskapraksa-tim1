using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RBBH.ConnectedParties.API.Controllers;
using RBBH.ConnectedParties.BL.ServiceInterfaces;
using RBBH.ConnectedParties.BL.Services;
using RBBH.ConnectedParties.DL.DTO.Users;
using RBBH.ConnectedParties.DL.Entities.Role;
using RBBH.ConnectedParties.DL.Entities.Users;
using RBBH.ConnectedParties.DL.Persistence;
using RBBH.ConnectedParties.Helpers.Constants;
using RBBH.ConnectedParties.IoC.Extensions.Authentication;

namespace IntegrationTests.Tests.Users;

// Real HTTP/controller/service/audit/DB authorization path. Only identity and
// unique InMemory state are fixtures; never backend Program/.env or SQL/SSO.
public abstract class UsersHttpFixture : IAsyncLifetime
{
    protected const string Subject = "e2726e31-cf86-474f-a59f-05f80ab19c36";
    protected const string Email = "admin-fixture@example.test";
    protected readonly Guid _localId = Guid.NewGuid();
    protected WebApplication _app = null!;
    protected HttpClient _client = null!;
    protected readonly TaskCompletionSource _bothAuthenticated = new(TaskCreationOptions.RunContinuationsAsynchronously);
    protected int _raceArrivals;

    private readonly string _inMemoryName = "users-self-removal-discovery-" + Guid.NewGuid();

    /// <summary>Default store is a unique InMemory database; SQL fixtures override these.</summary>
    protected virtual void ConfigureDatabase(DbContextOptionsBuilder options) => options.UseInMemoryDatabase(_inMemoryName);
    protected virtual Task PrepareDatabaseAsync(ConnectedPartiesDbContext db) => Task.CompletedTask;
    protected virtual Task CleanUpDatabaseAsync() => Task.CompletedTask;

    public async Task InitializeAsync()
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            EnvironmentName = "Testing",
            ApplicationName = typeof(UsersController).Assembly.FullName
        });
        builder.WebHost.UseTestServer();
        builder.Logging.ClearProviders();
        builder.Configuration["KeycloakSettings:Enabled"] = "false";
        builder.Services.AddControllers().AddApplicationPart(typeof(UsersController).Assembly);
        builder.Services.AddDbContext<ConnectedPartiesDbContext>(ConfigureDatabase);
        builder.Services.AddAuthentication("UsersFixture")
            .AddScheme<AuthenticationSchemeOptions, IdentityFixture>("UsersFixture", _ => { });
        builder.Services.AddScoped<IClaimsTransformation, RoleClaimsTransformation>();
        builder.Services.AddApplicationAuthorization();
        builder.Services.AddScoped<IAppUserService, AppUserService>();
        builder.Services.AddScoped<IAuditService, AuditService>();
        builder.Services.AddHttpClient<KeycloakAdminService>().ConfigurePrimaryHttpMessageHandler(() => new NoOutboundRequests());
        _app = builder.Build();
        _app.UseAuthentication();
        _app.Use(async (context, next) =>
        {
            if (context.Request.Headers.ContainsKey("X-Test-Race"))
            {
                // Both requests must already have their DB authorization snapshot
                // before either mutation; no scheduler assumptions or sleeps.
                if (Interlocked.Increment(ref _raceArrivals) == 2) _bothAuthenticated.TrySetResult();
                await _bothAuthenticated.Task.WaitAsync(context.RequestAborted);
            }
            await next();
        });
        _app.UseAuthorization();
        _app.MapControllers();
        await _app.StartAsync();
        _client = _app.GetTestClient();

        await using var scope = _app.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ConnectedPartiesDbContext>();
        await PrepareDatabaseAsync(db);
        await DevelopmentDataSeeder.EnsureApplicationRolesAsync(db);
        var admin = await db.Roles.SingleAsync(role => role.Name == ApplicationAccessRoles.Administrator);
        db.AppUsers.Add(new AppUser
        {
            Id = _localId,
            KeycloakId = Subject,
            KeycloakSubjectConfirmed = true,
            Email = Email,
            Username = "admin-fixture",
            FirstName = "Synthetic",
            LastName = "Administrator",
            IsActive = true
        });
        db.UserRoles.Add(new UserRole { UserId = _localId, RoleId = admin.Id });
        await db.SaveChangesAsync();
    }

    protected async Task<Guid> ListedOwnIdAsync()
    {
        var response = await _client.GetAsync("/api/users");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<GetUsersResponseDTO>();
        var own = Assert.Single(result!.Users);
        Assert.Equal(_localId, own.Id);
        Assert.NotEqual(Subject, own.Id.ToString());
        return own.Id;
    }

    protected async Task<Guid> RoleId(string role)
    {
        await using var scope = _app.Services.CreateAsyncScope();
        return (await scope.ServiceProvider.GetRequiredService<ConnectedPartiesDbContext>().Roles.SingleAsync(item => item.Name == role)).Id;
    }

    protected async Task<Guid> AddUser(string role, string email)
    {
        await using var scope = _app.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ConnectedPartiesDbContext>();
        var user = new AppUser { Email = email, Username = email, KeycloakSubjectConfirmed = true, KeycloakId = Guid.NewGuid().ToString(), FirstName = "Synthetic", LastName = "Other" };
        db.AppUsers.Add(user);
        db.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = await RoleId(role) });
        await db.SaveChangesAsync();
        return user.Id;
    }

    protected async Task AddDatabaseOperator()
    {
        // A deliberately database-authorized non-Admin operator isolates the
        // final-Admin guard from self detection; no production role matrix edits.
        await AddUser(ApplicationAccessRoles.Limits, "operator@example.test");
        await using var scope = _app.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ConnectedPartiesDbContext>();
        var roleId = await RoleId(ApplicationAccessRoles.Limits);
        _client.DefaultRequestHeaders.Add("X-Test-Subject", (await db.AppUsers.SingleAsync(user => user.Email == "operator@example.test")).KeycloakId);
        foreach (var code in new[] { ApplicationPermissions.AdministrationView, ApplicationPermissions.AdministrationManage })
            db.RolePermissions.Add(new RolePermission { RoleId = roleId, PermissionId = (await db.Permissions.SingleAsync(permission => permission.Code == code)).Id });
        await db.SaveChangesAsync();
    }

    protected async Task<HttpResponseMessage> Mutate(HttpClient client, Guid id, string action) => action switch
    {
        "deactivate" => await client.DeleteAsync($"/api/users/{id}"),
        "delete" => await client.DeleteAsync($"/api/users/{id}/permanent"),
        _ => await client.PutAsJsonAsync($"/api/users/{id}/role", new { roleIds = action == "empty" ? [] : new[] { await RoleId(ApplicationAccessRoles.LegalPersons) } })
    };

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _app.DisposeAsync();
        await CleanUpDatabaseAsync();
    }

    private sealed class NoOutboundRequests : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            throw new InvalidOperationException("This isolated discovery must never call Keycloak or another network service.");
    }

    private sealed class IdentityFixture(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder)
        : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
    {
        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var subject = Request.Headers["X-Test-Subject"].FirstOrDefault() ?? Subject;
            var raw = Request.Headers.ContainsKey("X-Test-Raw-Claims");
            var claims = new List<Claim>
            {
                new(raw ? "sub" : ClaimTypes.NameIdentifier, subject),
                new(ClaimTypes.Email, Request.Headers["X-Test-Email"].FirstOrDefault() ?? Email), new(ClaimTypes.Name, "admin-fixture")
            };
            if (!Request.Headers.ContainsKey("X-Test-Unverified-Email"))
                claims.Add(new("email_verified", "true"));
            if (Request.Headers.ContainsKey("X-Test-Forge"))
                claims.AddRange([new(ClaimTypes.Role, ApplicationAccessRoles.Administrator), new("permission", ApplicationPermissions.AdministrationManage), new("rct_done", "1")]);
            return Task.FromResult(AuthenticateResult.Success(new(new ClaimsPrincipal(new ClaimsIdentity(claims, "UsersFixture")), "UsersFixture")));
        }
    }
}
