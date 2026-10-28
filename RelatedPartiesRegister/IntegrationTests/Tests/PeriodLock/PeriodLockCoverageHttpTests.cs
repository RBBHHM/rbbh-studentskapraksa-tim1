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
using Moq;
using RBBH.ConnectedParties.API.Controllers;
using RBBH.ConnectedParties.BL.ServiceInterfaces;
using RBBH.ConnectedParties.BL.Services;
using RBBH.ConnectedParties.DL.Entities.PeriodLock;
using RBBH.ConnectedParties.DL.Entities.Role;
using RBBH.ConnectedParties.DL.Entities.Users;
using RBBH.ConnectedParties.DL.Persistence;
using RBBH.ConnectedParties.Helpers.Constants;
using RBBH.ConnectedParties.IoC.Extensions.Authentication;
using RBBH.ConnectedParties.Middlewares;

namespace IntegrationTests.Tests.PeriodLock;

// Real middleware order (authentication -> authorization -> period lock), real controllers,
// real PeriodLockRepository and the DB-owned Admin grants. InMemory store only.
public sealed class PeriodLockCoverageHttpTests : IAsyncLifetime
{
    private WebApplication _app = null!;
    private HttpClient _client = null!;

    public async Task InitializeAsync()
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            EnvironmentName = "Testing",
            ApplicationName = typeof(CapitalController).Assembly.FullName
        });
        builder.WebHost.UseTestServer();
        builder.Logging.ClearProviders();
        builder.Services.AddControllers().AddApplicationPart(typeof(CapitalController).Assembly);
        var database = "period-lock-coverage-" + Guid.NewGuid();
        builder.Services.AddDbContext<ConnectedPartiesDbContext>(options => options.UseInMemoryDatabase(database));
        builder.Services.AddScoped<IClaimsTransformation, RoleClaimsTransformation>();
        builder.Services.AddAuthentication("PeriodFixture")
            .AddScheme<AuthenticationSchemeOptions, IdentityFixture>("PeriodFixture", _ => { });
        builder.Services.AddApplicationAuthorization();
        builder.Services.AddScoped<IPeriodLockRepository, PeriodLockRepository>();
        builder.Services.AddScoped<ICapitalService, CapitalService>();
        builder.Services.AddSingleton(Mock.Of<IAuditService>());
        builder.Services.AddSingleton(Mock.Of<IRelatedPersonService>());
        builder.Services.AddSingleton(Mock.Of<ILegalEntityService>());
        _app = builder.Build();
        _app.UseAuthentication();
        _app.UseAuthorization();
        _app.UseMiddleware<PeriodLockMiddleware>();
        _app.MapControllers();
        await _app.StartAsync();
        _client = _app.GetTestClient();

        await using var scope = _app.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ConnectedPartiesDbContext>();
        await DevelopmentDataSeeder.EnsureApplicationRolesAsync(db);
        var admin = new AppUser { Email = "admin@example.test", Username = "admin", KeycloakSubjectConfirmed = true, KeycloakId = "period-admin-subject" };
        db.AppUsers.Add(admin);
        db.UserRoles.Add(new UserRole { UserId = admin.Id, RoleId = (await db.Roles.SingleAsync(role => role.Name == ApplicationAccessRoles.Administrator)).Id });
        await db.SaveChangesAsync();
    }

    private async Task SetGlobalLockAsync(bool locked)
    {
        await using var scope = _app.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ConnectedPartiesDbContext>();
        var now = DateTime.UtcNow;
        var row = await db.PeriodLocks.FirstOrDefaultAsync(item => item.Year == now.Year && item.Month == now.Month && item.Department == null);
        if (row is null) db.PeriodLocks.Add(new RBBH.ConnectedParties.DL.Entities.PeriodLock.PeriodLock { Year = now.Year, Month = now.Month, IsLocked = locked, CreatedBy = "test" });
        else row.IsLocked = locked;
        await db.SaveChangesAsync();
    }

    private static object Capital() => new { regulatoryCapital = 100m, coreCapital = 60m, supplementaryCapital = 40m, capitalDate = DateTime.UtcNow.Date };

    [Fact]
    public async Task CapitalMutationIsBlockedWhileLockedAndAllowedAfterUnlock()
    {
        await SetGlobalLockAsync(true);
        var blocked = await _client.PutAsJsonAsync("/api/capital", Capital());
        Assert.Equal(HttpStatusCode.Locked, blocked.StatusCode);
        await using (var scope = _app.Services.CreateAsyncScope())
            Assert.Empty(await scope.ServiceProvider.GetRequiredService<ConnectedPartiesDbContext>().Set<RBBH.ConnectedParties.DL.Entities.Report.Capital>().ToListAsync());

        await SetGlobalLockAsync(false);
        Assert.Equal(HttpStatusCode.OK, (await _client.PutAsJsonAsync("/api/capital", Capital())).StatusCode);
    }

    [Theory]
    [InlineData("/api/verification/physical-person/")]
    [InlineData("/api/verification/legal-person/")]
    public async Task VerificationIsBlockedWhileLocked(string route)
    {
        await SetGlobalLockAsync(true);
        var response = await _client.PostAsync(route + Guid.NewGuid(), null);
        Assert.Equal(HttpStatusCode.Locked, response.StatusCode);
        Assert.Contains("\"errors\"", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task ReadsAndAdministrationStayAvailableWhileLocked()
    {
        await SetGlobalLockAsync(true);
        Assert.Contains((await _client.GetAsync("/api/capital")).StatusCode, new[] { HttpStatusCode.OK, HttpStatusCode.NoContent });
        Assert.NotEqual(HttpStatusCode.Locked, (await _client.GetAsync("/api/capital/export")).StatusCode);
        // Period administration, reporting and user administration must pass the lock middleware.
        // Their controllers are not wired in this fixture, so reaching controller activation
        // (an activation failure) is itself the proof that the request was not answered with 423.
        await AssertNotBlockedByLockAsync(() => _client.PostAsJsonAsync("/api/period-lock/unlock", new { }));
        await AssertNotBlockedByLockAsync(() => _client.PostAsJsonAsync("/api/reports/daily", new { }));
        await AssertNotBlockedByLockAsync(() => _client.PostAsJsonAsync("/api/users", new { }));
    }

    private static async Task AssertNotBlockedByLockAsync(Func<Task<HttpResponseMessage>> send)
    {
        try { Assert.NotEqual(HttpStatusCode.Locked, (await send()).StatusCode); }
        catch (InvalidOperationException error) when (error.Message.Contains("Unable to resolve service")) { }
    }

    [Theory]
    [InlineData("//api/capital")]
    [InlineData("/api//capital")]
    [InlineData("/api/capital/")]
    public async Task UnusualPathSpellingsCannotBypassTheLock(string path)
    {
        await SetGlobalLockAsync(true);
        var response = await _client.PutAsJsonAsync(path, Capital());
        // Either the request is rejected by the lock, or it never reaches the controller.
        Assert.True(response.StatusCode is HttpStatusCode.Locked or HttpStatusCode.NotFound or HttpStatusCode.BadRequest,
            $"{path} returned {(int)response.StatusCode}");
        await using var scope = _app.Services.CreateAsyncScope();
        Assert.Empty(await scope.ServiceProvider.GetRequiredService<ConnectedPartiesDbContext>().Set<RBBH.ConnectedParties.DL.Entities.Report.Capital>().ToListAsync());
    }

    [Theory]
    [InlineData("FL.fixture")]
    [InlineData("PL.fixture")]
    [InlineData("unprefixed")]
    public async Task UsernamePrefixesCannotChangeTheGlobalLock(string username)
    {
        _client.DefaultRequestHeaders.Add("X-Test-Username", username);
        await SetGlobalLockAsync(true);
        Assert.Equal(HttpStatusCode.Locked, (await _client.PutAsJsonAsync("/api/capital", Capital())).StatusCode);
        await SetGlobalLockAsync(false);
        Assert.Equal(HttpStatusCode.OK, (await _client.PutAsJsonAsync("/api/capital", Capital())).StatusCode);
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _app.DisposeAsync();
    }

    private sealed class IdentityFixture(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder)
        : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
    {
        protected override Task<AuthenticateResult> HandleAuthenticateAsync() =>
            Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(new ClaimsIdentity(
            [
                new Claim("sub", "period-admin-subject"), new Claim(ClaimTypes.Email, "admin@example.test"), new Claim("email_verified", "true"),
                new Claim(ClaimTypes.Name, Request.Headers["X-Test-Username"].FirstOrDefault() ?? "admin"),
                new Claim("preferred_username", Request.Headers["X-Test-Username"].FirstOrDefault() ?? "admin")
            ], "PeriodFixture")), "PeriodFixture")));
    }
}
