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
using RBBH.ConnectedParties.DL.Entities.Role;
using RBBH.ConnectedParties.DL.Entities.Sifarnici;
using RBBH.ConnectedParties.DL.Entities.Users;
using RBBH.ConnectedParties.DL.Persistence;
using RBBH.ConnectedParties.Helpers.Constants;
using RBBH.ConnectedParties.IoC.Extensions.Authentication;

namespace IntegrationTests.Tests;

// Real HTTP serialization, DB authorization, service reference guard and audit.
// Unique InMemory storage and synthetic identity; no backend Program/SQL/SSO.
public sealed class CodeListHttpStatusTests : IAsyncLifetime
{
    private WebApplication _app = null!;
    private HttpClient _client = null!;
    private int _codeId;
    public async Task InitializeAsync()
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = "Testing", ApplicationName = typeof(CodeListController).Assembly.FullName });
        builder.WebHost.UseTestServer(); builder.Logging.ClearProviders();
        builder.Services.AddControllers().AddApplicationPart(typeof(CodeListController).Assembly);
        var name = "code-http-status-" + Guid.NewGuid();
        builder.Services.AddDbContext<ConnectedPartiesDbContext>(options => options.UseInMemoryDatabase(name));
        builder.Services.AddAuthentication("Fixture").AddScheme<AuthenticationSchemeOptions, IdentityFixture>("Fixture", _ => { });
        builder.Services.AddScoped<IClaimsTransformation, RoleClaimsTransformation>(); builder.Services.AddApplicationAuthorization();
        builder.Services.AddScoped<ICodeListService, CodeListService>(); builder.Services.AddScoped<IAuditService, AuditService>();
        _app = builder.Build(); _app.UseAuthentication(); _app.UseAuthorization(); _app.MapControllers();
        await _app.StartAsync(); _client = _app.GetTestClient();
        await using var scope = _app.Services.CreateAsyncScope(); var db = scope.ServiceProvider.GetRequiredService<ConnectedPartiesDbContext>();
        await DevelopmentDataSeeder.EnsureApplicationRolesAsync(db);
        var user = new AppUser { Email = "codes@example.test", Username = "codes", KeycloakSubjectConfirmed = true, KeycloakId = "codes-fixture" };
        db.AppUsers.Add(user); db.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = (await db.Roles.SingleAsync(role => role.Name == ApplicationAccessRoles.Administrator)).Id });
        var value = new CodeList { Kategorija = "OsnovPovezanosti", Kod = "REFERENCE", Naziv = "In use", KreiraoKorisnik = "fixture" }; db.CodeLists.Add(value);
        db.LegalEntities.Add(new() { Name = "Fixture company", BasisOfConnection = "REFERENCE", IsActive = true, CreatedBy = "fixture" });
        await db.SaveChangesAsync(); _codeId = value.ID;
    }
    [Theory]
    [InlineData("referenced-category", 400)]
    [InlineData("missing-category", 404)]
    [InlineData("invalid-update", 400)]
    [InlineData("missing-update", 404)]
    [InlineData("invalid-read", 400)]
    public async Task RejectedServiceResultsRemainActualHttpErrorsRatherThanSuccessEnvelopes(string operation, int expected)
    {
        using var response = operation switch
        {
            "referenced-category" => await _client.DeleteAsync("/api/code-lists/categories/OsnovPovezanosti"),
            "missing-category" => await _client.DeleteAsync("/api/code-lists/categories/Missing"),
            "invalid-update" => await _client.PutAsJsonAsync($"/api/code-lists/{_codeId}", new { naziv = " ", aktivan = false }),
            "missing-update" => await _client.PutAsJsonAsync("/api/code-lists/99999", new { naziv = "Valid", aktivan = false }),
            _ => await _client.GetAsync("/api/code-lists/%20")
        };
        Assert.Equal((HttpStatusCode)expected, response.StatusCode);
        using var body = System.Text.Json.JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(expected, body.RootElement.GetProperty("status").GetInt32());
        Assert.False(body.RootElement.TryGetProperty("result", out _));
        await using var scope = _app.Services.CreateAsyncScope(); var db = scope.ServiceProvider.GetRequiredService<ConnectedPartiesDbContext>();
        Assert.True((await db.CodeLists.IgnoreQueryFilters().SingleAsync()).Aktivan);
        Assert.Equal("In use", (await db.CodeLists.SingleAsync()).Naziv); Assert.Empty(await db.AuditLogs.ToListAsync());
    }
    public async Task DisposeAsync() { _client.Dispose(); await _app.DisposeAsync(); }
    private sealed class IdentityFixture(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder)
        : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
    {
        protected override Task<AuthenticateResult> HandleAuthenticateAsync() => Task.FromResult(AuthenticateResult.Success(new(
            new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, "codes-fixture"), new Claim(ClaimTypes.Email, "codes@example.test"), new Claim(ClaimTypes.Name, "codes")], "Fixture")), "Fixture")));
    }
}
