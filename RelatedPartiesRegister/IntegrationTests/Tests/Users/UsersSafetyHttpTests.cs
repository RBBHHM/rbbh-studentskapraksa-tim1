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
public sealed class UsersSafetyHttpTests : IAsyncLifetime
{
    private const string Subject = "e2726e31-cf86-474f-a59f-05f80ab19c36";
    private const string Email = "admin-fixture@example.test";
    private readonly Guid _localId = Guid.NewGuid();
    private WebApplication _app = null!;
    private HttpClient _client = null!;
    private readonly TaskCompletionSource _bothAuthenticated = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private int _raceArrivals;

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
        var database = "users-self-removal-discovery-" + Guid.NewGuid();
        builder.Services.AddDbContext<ConnectedPartiesDbContext>(options => options.UseInMemoryDatabase(database));
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
        await DevelopmentDataSeeder.EnsureApplicationRolesAsync(db);
        var admin = await db.Roles.SingleAsync(role => role.Name == ApplicationAccessRoles.Administrator);
        db.AppUsers.Add(new AppUser
        {
            Id = _localId,
            KeycloakId = Subject,
            Email = Email,
            Username = "admin-fixture",
            FirstName = "Synthetic",
            LastName = "Administrator",
            IsActive = true
        });
        db.UserRoles.Add(new UserRole { UserId = _localId, RoleId = admin.Id });
        await db.SaveChangesAsync();
    }

    private async Task<Guid> ListedOwnIdAsync()
    {
        var response = await _client.GetAsync("/api/users");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<GetUsersResponseDTO>();
        var own = Assert.Single(result!.Users);
        Assert.Equal(_localId, own.Id);
        Assert.NotEqual(Subject, own.Id.ToString());
        return own.Id;
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public async Task ExistingSelfProtectionRejectsLocalAndKeycloakIds(bool permanent, bool subjectRoute)
    {
        var id = await ListedOwnIdAsync();
        var response = await _client.DeleteAsync($"/api/users/{(subjectRoute ? Subject : id.ToString())}" + (permanent ? "/permanent" : ""));
        await using var scope = _app.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ConnectedPartiesDbContext>();
        var own = await db.AppUsers.AsNoTracking().SingleOrDefaultAsync(user => user.Id == id);
        var nextRequest = await _client.GetAsync("/api/users");
        Assert.True(response.StatusCode == HttpStatusCode.BadRequest && own?.IsActive == true,
            $"Expected 400 and preserved active self mapping; actual {(int)response.StatusCode}, " +
            $"exists={own is not null}, active={own?.IsActive}, next administration read={(int)nextRequest.StatusCode}.");
        Assert.Equal(HttpStatusCode.OK, nextRequest.StatusCode);
        Assert.Empty(await db.AuditLogs.ToListAsync());
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public async Task LastAdminRoleRemovalIsRejectedForBothExistingRoutes(bool empty, bool postAlias)
    {
        var id = await ListedOwnIdAsync();
        var payload = new { roleIds = empty ? [] : new[] { await RoleId(ApplicationAccessRoles.PhysicalPersons) } };
        var response = postAlias ? await _client.PostAsJsonAsync($"/api/users/{id}/roles", payload)
            : await _client.PutAsJsonAsync($"/api/users/{id}/role", payload);
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Contains("najmanje jedan aktivni Admin", await response.Content.ReadAsStringAsync());
        await using var scope = _app.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ConnectedPartiesDbContext>();
        Assert.True((await db.AppUsers.SingleAsync()).IsActive);
        Assert.True((await db.UserRoles.SingleAsync()).IsActive);
        Assert.Empty(await db.AuditLogs.ToListAsync());
        Assert.Equal(HttpStatusCode.OK, (await _client.GetAsync("/api/users")).StatusCode);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SelfDetectionAlsoPreservesTheCurrentEmailToDatabaseIdentityLink(bool permanent)
    {
        // Authorization currently resolves DB grants by trusted authenticated
        // email. A differing subject must not bypass protection of that mapping.
        _client.DefaultRequestHeaders.Add("X-Test-Subject", Guid.NewGuid().ToString());
        Assert.Equal(HttpStatusCode.BadRequest, (await _client.DeleteAsync($"/api/users/{_localId}" + (permanent ? "/permanent" : ""))).StatusCode);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task UnmappedOidcSubjectAndEmailClaimsAlsoProtectSelf(bool permanent)
    {
        _client.DefaultRequestHeaders.Add("X-Test-Raw-Claims", "true");
        Assert.Equal(HttpStatusCode.BadRequest, (await _client.DeleteAsync($"/api/users/{_localId}" + (permanent ? "/permanent" : ""))).StatusCode);
    }

    [Fact]
    public async Task KeepingAdminRoleAndReactivatingOrdinaryUserRemainAllowed()
    {
        var adminId = await RoleId(ApplicationAccessRoles.Administrator);
        Assert.Equal(HttpStatusCode.OK, (await _client.PutAsJsonAsync($"/api/users/{_localId}/role", new { roleIds = new[] { adminId } })).StatusCode);
        var other = await AddUser(ApplicationAccessRoles.PhysicalPersons, "other@example.test");
        Assert.Equal(HttpStatusCode.OK, (await Mutate(_client, other, "deactivate")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await _client.PostAsJsonAsync($"/api/users/{other}/reactivate", new { })).StatusCode);
        await using var scope = _app.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ConnectedPartiesDbContext>();
        Assert.True((await db.AppUsers.SingleAsync(user => user.Id == other)).IsActive);
        Assert.Equal(new[] { "ROLE_ASSIGN", "DEACTIVATE", "REACTIVATE" }, (await db.AuditLogs.OrderBy(log => log.Timestamp).Select(log => log.Action).ToArrayAsync()));
    }

    [Fact]
    public async Task InvalidRolesAndMissingTargetsStillReturnExistingSafeResultsWithoutAudit()
    {
        Assert.Equal(HttpStatusCode.BadRequest, (await _client.PutAsJsonAsync($"/api/users/{_localId}/role", new { roleIds = new[] { Guid.NewGuid() } })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await Mutate(_client, Guid.NewGuid(), "delete")).StatusCode);
        await using var scope = _app.Services.CreateAsyncScope();
        Assert.Empty(await scope.ServiceProvider.GetRequiredService<ConnectedPartiesDbContext>().AuditLogs.ToListAsync());
    }

    [Theory]
    [InlineData("deactivate")]
    [InlineData("delete")]
    public async Task NonSelfWriterCannotRemoveLastAdmin(string action)
    {
        await AddDatabaseOperator();
        _client.DefaultRequestHeaders.Add("X-Test-Email", "operator@example.test");
        Assert.Equal(HttpStatusCode.Conflict, (await Mutate(_client, _localId, action)).StatusCode);
        await using var scope = _app.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ConnectedPartiesDbContext>();
        Assert.True((await db.AppUsers.SingleAsync(user => user.Id == _localId)).IsActive);
        Assert.Empty(await db.AuditLogs.ToListAsync());
    }

    [Theory]
    [InlineData("role", true)]
    [InlineData("empty", true)]
    [InlineData("deactivate", true)]
    [InlineData("delete", true)]
    [InlineData("role", false)]
    [InlineData("empty", false)]
    [InlineData("deactivate", false)]
    [InlineData("delete", false)]
    public async Task OtherAdminOrOrdinaryUserCanChangeAndAuditRemainsCorrect(string action, bool admin)
    {
        var id = await AddUser(admin ? ApplicationAccessRoles.Administrator : ApplicationAccessRoles.PhysicalPersons, "other@example.test");
        Assert.Equal(HttpStatusCode.OK, (await Mutate(_client, id, action)).StatusCode);
        await using var scope = _app.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ConnectedPartiesDbContext>();
        Assert.True((await db.AppUsers.SingleAsync(user => user.Id == _localId)).IsActive);
        var changed = await db.AppUsers.SingleOrDefaultAsync(user => user.Id == id);
        if (action is "empty" or "delete") Assert.Null(changed);
        else if (action == "deactivate") Assert.False(changed!.IsActive);
        else
        {
            Assert.True(changed!.IsActive);
            Assert.False(await db.UserRoles.AnyAsync(mapping => mapping.UserId == id && mapping.IsActive && mapping.Role.Name == ApplicationAccessRoles.Administrator));
        }
        var audit = Assert.Single(await db.AuditLogs.ToListAsync());
        Assert.Equal("AppUser", audit.TableName);
        Assert.Equal(id.ToString(), audit.RecordId);
        Assert.Equal("admin-fixture", audit.Username);
        Assert.Equal(action switch { "role" => "ROLE_ASSIGN", "empty" => "ROLE_REMOVE", "deactivate" => "DEACTIVATE", _ => "DELETE" }, audit.Action);
        if (action == "role") Assert.Contains(ApplicationAccessRoles.LegalPersons, audit.NewValues);
        if (action == "deactivate") { Assert.Contains("true", audit.OldValues); Assert.Contains("false", audit.NewValues); }
        Assert.Equal(HttpStatusCode.OK, (await _client.GetAsync("/api/users")).StatusCode);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task InactiveUsersOrAssignmentsDoNotCountAsRemainingAdmins(bool inactiveUser)
    {
        var id = await AddUser(ApplicationAccessRoles.Administrator, "inactive@example.test");
        await using (var scope = _app.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ConnectedPartiesDbContext>();
            if (inactiveUser) (await db.AppUsers.SingleAsync(user => user.Id == id)).IsActive = false;
            else (await db.UserRoles.SingleAsync(mapping => mapping.UserId == id)).IsActive = false;
            await db.SaveChangesAsync();
        }
        Assert.Equal(HttpStatusCode.Conflict, (await Mutate(_client, _localId, "empty")).StatusCode);
    }

    [Fact]
    public async Task ConcurrentRoleRemovalCannotRemoveBothActiveAdmins()
    {
        var otherId = await AddUser(ApplicationAccessRoles.Administrator, "other@example.test");
        using var second = _app.GetTestClient();
        second.DefaultRequestHeaders.Add("X-Test-Email", "other@example.test");
        _client.DefaultRequestHeaders.Add("X-Test-Race", "true");
        second.DefaultRequestHeaders.Add("X-Test-Race", "true");
        var outcomes = await Task.WhenAll(Mutate(_client, _localId, "empty"), Mutate(second, otherId, "empty"));
        Assert.Single(outcomes, result => result.StatusCode == HttpStatusCode.OK);
        Assert.Single(outcomes, result => result.StatusCode == HttpStatusCode.Conflict);
        await using var scope = _app.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ConnectedPartiesDbContext>();
        Assert.Single(await db.AppUsers.Where(user => user.IsActive).ToListAsync());
        Assert.Single(await db.UserRoles.Where(mapping => mapping.IsActive && mapping.Role.Name == ApplicationAccessRoles.Administrator).ToListAsync());
        Assert.Single(await db.AuditLogs.ToListAsync());
    }

    [Fact]
    public async Task UnmappedIdentityCannotAcquireAdministrationFromProviderClaims()
    {
        _client.DefaultRequestHeaders.Add("X-Test-Email", "ordinary@example.test");
        _client.DefaultRequestHeaders.Add("X-Test-Forge", "true");
        Assert.Equal(HttpStatusCode.Forbidden, (await Mutate(_client, _localId, "empty")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await Mutate(_client, _localId, "delete")).StatusCode);
        await using var scope = _app.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ConnectedPartiesDbContext>();
        Assert.True(await db.AppUsers.AnyAsync(user => user.Id == _localId && user.IsActive));
        Assert.Empty(await db.AuditLogs.ToListAsync());
    }

    private async Task<Guid> RoleId(string role)
    {
        await using var scope = _app.Services.CreateAsyncScope();
        return (await scope.ServiceProvider.GetRequiredService<ConnectedPartiesDbContext>().Roles.SingleAsync(item => item.Name == role)).Id;
    }

    private async Task<Guid> AddUser(string role, string email)
    {
        await using var scope = _app.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ConnectedPartiesDbContext>();
        var user = new AppUser { Email = email, Username = email, KeycloakId = Guid.NewGuid().ToString(), FirstName = "Synthetic", LastName = "Other" };
        db.AppUsers.Add(user);
        db.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = await RoleId(role) });
        await db.SaveChangesAsync();
        return user.Id;
    }

    private async Task AddDatabaseOperator()
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

    private async Task<HttpResponseMessage> Mutate(HttpClient client, Guid id, string action) => action switch
    {
        "deactivate" => await client.DeleteAsync($"/api/users/{id}"),
        "delete" => await client.DeleteAsync($"/api/users/{id}/permanent"),
        _ => await client.PutAsJsonAsync($"/api/users/{id}/role", new { roleIds = action == "empty" ? [] : new[] { await RoleId(ApplicationAccessRoles.LegalPersons) } })
    };

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _app.DisposeAsync();
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
                new(raw ? "email" : ClaimTypes.Email, Request.Headers["X-Test-Email"].FirstOrDefault() ?? Email), new(ClaimTypes.Name, "admin-fixture")
            };
            if (Request.Headers.ContainsKey("X-Test-Forge"))
                claims.AddRange([new(ClaimTypes.Role, ApplicationAccessRoles.Administrator), new("permission", ApplicationPermissions.AdministrationManage), new("rct_done", "1")]);
            return Task.FromResult(AuthenticateResult.Success(new(new ClaimsPrincipal(new ClaimsIdentity(claims, "UsersFixture")), "UsersFixture")));
        }
    }
}
