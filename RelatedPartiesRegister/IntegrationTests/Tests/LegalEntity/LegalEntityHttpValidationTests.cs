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
using RBBH.ConnectedParties.DL.DTO.LegalEntity;
using RBBH.ConnectedParties.DL.Entities.Role;
using RBBH.ConnectedParties.DL.Entities.Users;
using RBBH.ConnectedParties.DL.Persistence;
using RBBH.ConnectedParties.Helpers.Constants;
using RBBH.ConnectedParties.IoC.Extensions.Authentication;
using RBBH.ConnectedParties.IoC.Extensions.ExceptionHandling;

namespace IntegrationTests.Tests.LegalEntity;

public sealed class LegalEntityHttpValidationTests : IAsyncLifetime
{
    private WebApplication _app = null!;
    private HttpClient _client = null!;
    private static CreateLegalEntityDTO Valid() => new()
    {
        Name = "Fixture Company",
        IsResident = false,
        FbaId = "123456",
        MaticniBroj = "12345",
        BasisOfConnection = "ZOB-2-V-5",
        ConnectionDescription = "Fixture relationship",
        ConnectedWithBank = true,
        DateFrom = new(2026, 1, 1),
        DateTo = new(2026, 12, 31)
    };

    public async Task InitializeAsync()
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = "Testing", ApplicationName = typeof(LegalEntityController).Assembly.FullName });
        builder.WebHost.UseTestServer(); builder.Logging.ClearProviders();
        builder.Services.AddControllers().AddApplicationPart(typeof(LegalEntityController).Assembly);
        builder.Services.AddExceptionHandlingExtension(builder.Environment);
        var database = "legal-http-" + Guid.NewGuid();
        builder.Services.AddDbContext<ConnectedPartiesDbContext>(options => options.UseInMemoryDatabase(database));
        builder.Services.AddAuthentication("LegalFixture").AddScheme<AuthenticationSchemeOptions, IdentityFixture>("LegalFixture", _ => { });
        builder.Services.AddScoped<IClaimsTransformation, RoleClaimsTransformation>();
        builder.Services.AddApplicationAuthorization();
        builder.Services.AddScoped<ILegalEntityService, LegalEntityService>();
        builder.Services.AddSingleton(Mock.Of<IAuditService>());
        _app = builder.Build(); _app.UseExceptionHandler(); _app.UseAuthentication(); _app.UseAuthorization(); _app.MapControllers();
        _app.MapGet("/__fixture/unexpected", () => { throw new InvalidOperationException("SQL private-fixture-marker"); }).RequireAuthorization();
        await _app.StartAsync(); _client = _app.GetTestClient();
        await using var scope = _app.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ConnectedPartiesDbContext>();
        await DevelopmentDataSeeder.EnsureApplicationRolesAsync(db);
        var user = new AppUser { Email = "pl@example.test", Username = "fixture", KeycloakId = "fixture" };
        db.AppUsers.Add(user);
        db.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = (await db.Roles.SingleAsync(role => role.Name == ApplicationAccessRoles.LegalPersons)).Id });
        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task DuplicateFbaIsFieldValidationNotServerFailureAndDoesNotPersist()
    {
        Assert.Equal(HttpStatusCode.Created, (await _client.PostAsJsonAsync("/api/legal-entities", Valid())).StatusCode);
        var duplicate = await _client.PostAsJsonAsync("/api/legal-entities", Valid());
        Assert.Equal(HttpStatusCode.BadRequest, duplicate.StatusCode);
        using var json = JsonDocument.Parse(await duplicate.Content.ReadAsStringAsync());
        Assert.True(json.RootElement.GetProperty("errors").TryGetProperty("fbaId", out _));
        Assert.DoesNotContain("123456", await duplicate.Content.ReadAsStringAsync());
        await using var scope = _app.Services.CreateAsyncScope();
        Assert.Equal(1, await scope.ServiceProvider.GetRequiredService<ConnectedPartiesDbContext>().LegalEntities.CountAsync());
    }

    [Fact]
    public async Task InvalidBusinessDateRangeIdentifiesDateFieldAndDoesNotPersist()
    {
        var request = Valid(); request.DateTo = request.DateFrom!.Value.AddDays(-1);
        var response = await _client.PostAsJsonAsync("/api/legal-entities", request);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.True(json.RootElement.GetProperty("errors").TryGetProperty("dateTo", out _));
        await using var scope = _app.Services.CreateAsyncScope();
        Assert.Equal(0, await scope.ServiceProvider.GetRequiredService<ConnectedPartiesDbContext>().LegalEntities.CountAsync());
    }

    [Fact]
    public async Task UnexpectedFailuresStillRedactInternalDetails()
    {
        var response = await _client.GetAsync("/__fixture/unexpected");
        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("private-fixture-marker", body); Assert.DoesNotContain("stackTrace", body);
    }

    public async Task DisposeAsync() { _client.Dispose(); await _app.DisposeAsync(); }
    private sealed class IdentityFixture(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder)
        : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
    {
        protected override Task<AuthenticateResult> HandleAuthenticateAsync() => Task.FromResult(AuthenticateResult.Success(new(
            new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Email, "pl@example.test"), new Claim(ClaimTypes.Name, "fixture")], "LegalFixture")), "LegalFixture")));
    }
}
