using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.Json;
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
using RBBH.ConnectedParties.DL.Entities.Role;
using RBBH.ConnectedParties.DL.Entities.Users;
using RBBH.ConnectedParties.DL.Persistence;
using RBBH.ConnectedParties.Helpers.Constants;
using RBBH.ConnectedParties.IoC.Extensions.Authentication;

namespace IntegrationTests.Tests.Report;

// Actual HTTP authentication/authorization + production controllers/services and
// DB role transformation. Only identity and the EF store are local test fixtures.
// Program/.env, SQL Server, Keycloak, SMTP and hosted jobs are never started.
public sealed class ReportHttpAuthorizationTests : IAsyncLifetime
{
    private WebApplication _app = null!;
    private HttpClient _client = null!;
    private Guid _existingReport;

    public async Task InitializeAsync()
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            EnvironmentName = "Testing",
            ApplicationName = typeof(ReportController).Assembly.FullName
        });
        builder.WebHost.UseTestServer();
        builder.Logging.ClearProviders();
        builder.Services.AddControllers().AddApplicationPart(typeof(ReportController).Assembly);
        var database = "report-http-" + Guid.NewGuid();
        builder.Services.AddDbContext<ConnectedPartiesDbContext>(options => options.UseInMemoryDatabase(database));
        builder.Services.AddScoped<IClaimsTransformation, RoleClaimsTransformation>();
        builder.Services.AddAuthentication(IdentityFixture.Scheme)
            .AddScheme<AuthenticationSchemeOptions, IdentityFixture>(IdentityFixture.Scheme, _ => { });
        builder.Services.AddApplicationAuthorization();
        builder.Services.AddScoped<IReportService, ReportService>();
        builder.Services.AddSingleton(Mock.Of<IAuditService>());
        _app = builder.Build();
        _app.UseAuthentication();
        _app.UseAuthorization();
        _app.MapControllers();
        await _app.StartAsync();
        _client = _app.GetTestClient();
        await using var scope = _app.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ConnectedPartiesDbContext>();
        await DevelopmentDataSeeder.EnsureApplicationRolesAsync(db);
        foreach (var roleName in new[] { ApplicationAccessRoles.Capital, ApplicationAccessRoles.Administrator,
                     ApplicationAccessRoles.PhysicalPersons, ApplicationAccessRoles.LegalPersons, ApplicationAccessRoles.Limits })
        {
            var user = new AppUser { Email = roleName + "@example.test", Username = roleName, KeycloakSubjectConfirmed = true, KeycloakId = "subject:" + roleName + "@example.test" };
            var role = await db.Roles.SingleAsync(role => role.Name == roleName);
            db.AppUsers.Add(user);
            db.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = role.Id });
        }
        // Known active user with no explicit functional role: Guest baseline.
        db.AppUsers.Add(new AppUser { Email = "ordinary@example.test", Username = "ordinary", KeycloakSubjectConfirmed = true, KeycloakId = "subject:ordinary@example.test" });
        await db.SaveChangesAsync();
        _existingReport = (await scope.ServiceProvider.GetRequiredService<IReportService>()
            .GenerateDailyReportAsync("existing-author")).Id;
    }

    [Fact]
    public async Task OrdinaryAuthenticatedUserReadsAndDownloadsButCannotGenerateOrPersist()
    {
        _client.DefaultRequestHeaders.Add("X-Test-Identity", "ordinary@example.test");
        Assert.Equal(HttpStatusCode.OK, (await _client.GetAsync("/api/reports/daily")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await _client.GetAsync("/api/reports/monthly")).StatusCode);
        var download = await _client.GetAsync($"/api/reports/export/generated/{_existingReport}");
        Assert.Equal(HttpStatusCode.OK, download.StatusCode);
        Assert.Equal("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", download.Content.Headers.ContentType?.MediaType);
        Assert.Equal(new byte[] { 0x50, 0x4b }, (await download.Content.ReadAsByteArrayAsync()).Take(2).ToArray());
        Assert.Equal(HttpStatusCode.Forbidden, (await _client.PostAsJsonAsync("/api/reports/daily", new { })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await _client.PostAsJsonAsync($"/api/reports/monthly/{DateTime.UtcNow.Year}/3", new { })).StatusCode);
        await AssertStoredCountAsync(1);
    }

    [Theory]
    [InlineData("GET", "/api/reports/daily")]
    [InlineData("GET", "/api/reports/monthly")]
    [InlineData("GET", "/api/reports/export/all-clients")]
    [InlineData("POST", "/api/reports/daily")]
    public async Task AuthenticatedUnmappedIdentityReadsAndDownloadsAsGuestButCannotGenerate(string method, string path)
    {
        _client.DefaultRequestHeaders.Add("X-Test-Identity", "stranger@example.test");
        var response = method == "GET" ? await _client.GetAsync(path) : await _client.PostAsJsonAsync(path, new { });
        Assert.Equal(method == "GET" ? HttpStatusCode.OK : HttpStatusCode.Forbidden, response.StatusCode);
        var download = await _client.GetAsync($"/api/reports/export/generated/{_existingReport}");
        Assert.Equal(HttpStatusCode.OK, download.StatusCode);
        await AssertStoredCountAsync(1);
    }

    [Theory]
    [InlineData(ApplicationAccessRoles.Capital, "daily")]
    [InlineData(ApplicationAccessRoles.Capital, "monthly")]
    [InlineData(ApplicationAccessRoles.Administrator, "daily")]
    [InlineData(ApplicationAccessRoles.Administrator, "monthly")]
    public async Task DatabaseKapitalAndAdminGenerateAndPersist(string role, string type)
    {
        _client.DefaultRequestHeaders.Add("X-Test-Identity", role + "@example.test");
        var path = type == "daily" ? "/api/reports/daily" : $"/api/reports/monthly/{DateTime.UtcNow.Year}/3";
        var response = await _client.PostAsJsonAsync(path, new { });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var id = json.RootElement.GetProperty("id").GetGuid();
        await using var scope = _app.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ConnectedPartiesDbContext>();
        var persisted = await db.Reports.AsNoTracking().SingleAsync(report => report.Id == id);
        Assert.Equal(type.ToUpperInvariant(), persisted.ReportType);
        Assert.Equal(role + "@example.test", persisted.CreatedBy);
        Assert.False(string.IsNullOrWhiteSpace(persisted.DataSnapshot));
        Assert.Equal(2, await db.Reports.CountAsync());
    }

    [Theory]
    [InlineData(ApplicationAccessRoles.PhysicalPersons)]
    [InlineData(ApplicationAccessRoles.LegalPersons)]
    [InlineData(ApplicationAccessRoles.Limits)]
    public async Task OtherDatabaseRolesCannotGenerate(string role)
    {
        _client.DefaultRequestHeaders.Add("X-Test-Identity", role + "@example.test");
        Assert.Equal(HttpStatusCode.Forbidden, (await _client.PostAsJsonAsync("/api/reports/daily", new { })).StatusCode);
        await AssertStoredCountAsync(1);
    }

    [Fact]
    public async Task IdentityProviderClaimsCannotGrantApplicationGeneratePermission()
    {
        _client.DefaultRequestHeaders.Add("X-Test-Identity", "ordinary@example.test");
        _client.DefaultRequestHeaders.Add("X-Test-Untrusted-Grants", "true");
        Assert.Equal(HttpStatusCode.Forbidden, (await _client.PostAsJsonAsync("/api/reports/daily", new { })).StatusCode);
        await AssertStoredCountAsync(1);
    }

    [Theory]
    [InlineData("/api/reports/daily")]
    [InlineData("/api/reports/monthly")]
    [InlineData("/api/reports/export/all-clients")]
    public async Task AnonymousCannotReadOrDownload(string path) =>
        Assert.Equal(HttpStatusCode.Unauthorized, (await _client.GetAsync(path)).StatusCode);

    private async Task AssertStoredCountAsync(int expected)
    {
        await using var scope = _app.Services.CreateAsyncScope();
        Assert.Equal(expected, await scope.ServiceProvider.GetRequiredService<ConnectedPartiesDbContext>().Reports.CountAsync());
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _app.DisposeAsync();
    }

    private sealed class IdentityFixture(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder)
        : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
    {
        public new const string Scheme = "ReportIdentityFixture";
        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var email = Request.Headers["X-Test-Identity"].ToString();
            if (email.Length == 0) return Task.FromResult(AuthenticateResult.NoResult());
            var claims = new List<Claim> { new("sub", "subject:" + email), new(ClaimTypes.Email, email), new("email_verified", "true"), new(ClaimTypes.Name, email) };
            if (Request.Headers.ContainsKey("X-Test-Untrusted-Grants"))
            {
                claims.Add(new Claim(ClaimTypes.Role, ApplicationAccessRoles.Administrator));
                claims.Add(new Claim("permission", ApplicationPermissions.CapitalCreate));
                claims.Add(new Claim("rct_done", "1"));
            }
            return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(new ClaimsIdentity(claims, Scheme)), Scheme)));
        }
    }
}
