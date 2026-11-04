using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using RBBH.ConnectedParties.DL.Persistence;
using RBBH.ConnectedParties.IoC.Extensions.Authentication;

namespace IntegrationTests.Tests.Operations;

public sealed class AbsoluteSessionHttpTests : IAsyncLifetime
{
    private WebApplication _app = null!;
    private HttpClient _client = null!;
    private readonly ControlledClock _clock = new();

    public async Task InitializeAsync()
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = "Development" });
        builder.WebHost.UseTestServer();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["KeycloakSettings:Enabled"] = "true", ["KeycloakSettings:Issuer"] = "https://identity.example.test/realms/fixture",
            ["KeycloakSettings:ClientId"] = "fixture", ["KeycloakSettings:ClientSecret"] = "fixture-only", ["KeycloakSettings:Audience"] = "fixture-api"
        });
        var database = "session-" + Guid.NewGuid();
        builder.Services.AddDbContext<ConnectedPartiesDbContext>(options => options.UseInMemoryDatabase(database));
        builder.Services.AddAntiforgery(options => options.HeaderName = "X-CSRF-TOKEN");
        builder.Services.AddAuthenticationExtension(builder.Configuration, builder.Environment);
        builder.Services.PostConfigure<CookieAuthenticationOptions>("Cookies", options => options.TimeProvider = _clock);
        builder.Services.PostConfigure<OpenIdConnectOptions>("KeycloakOidc", options => options.ConfigurationManager = new Microsoft.IdentityModel.Protocols.StaticConfigurationManager<OpenIdConnectConfiguration>(new OpenIdConnectConfiguration
        { Issuer = "https://identity.example.test/realms/fixture", EndSessionEndpoint = "https://identity.example.test/logout" }));
        _app = builder.Build(); _app.UseAuthentication(); _app.UseAuthorization();
        _app.MapApplicationAuthentication();
        _app.MapPost("/fixture/login", async (HttpContext context, int? minutes, int? tokenMinutes) =>
        {
            var principal = new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub", "fixture-subject"),
                new Claim(ClaimTypes.Email, "fixture@example.test")], "Fixture"));
            var properties = new AuthenticationProperties { ExpiresUtc = _clock.GetUtcNow().AddMinutes(minutes ?? 900) };
            if (tokenMinutes is not null) properties.StoreTokens([
                new AuthenticationToken { Name = "access_token", Value = "fixture-opaque-token" },
                new AuthenticationToken { Name = "expires_at", Value = _clock.GetUtcNow().AddMinutes(tokenMinutes.Value).ToString("O") }]);
            await context.SignInAsync("Cookies", principal, properties);
            return Results.Ok();
        });
        _app.MapGet("/protected", () => "ok").RequireAuthorization();
        await _app.StartAsync(); _client = _app.GetTestClient();
    }

    private async Task SignIn(int minutes = 900)
    {
        var response = await _client.PostAsync($"/fixture/login?minutes={minutes}", null);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var cookie = response.Headers.GetValues("Set-Cookie").Single(value => value.StartsWith(".AspNetCore.Cookies="));
        _client.DefaultRequestHeaders.Add("Cookie", cookie.Split(';')[0]);
    }

    [Fact]
    public async Task ActivityCannotSlideTheCookieBeyondEightHours()
    {
        await SignIn(); _clock.Advance(TimeSpan.FromHours(7.99));
        Assert.Equal(HttpStatusCode.OK, (await _client.GetAsync("/protected")).StatusCode);
        _clock.Advance(TimeSpan.FromMinutes(1));
        Assert.Equal(HttpStatusCode.Unauthorized, (await _client.GetAsync("/protected")).StatusCode);
        var options = _app.Services.GetRequiredService<IOptionsMonitor<CookieAuthenticationOptions>>().Get("Cookies");
        Assert.False(options.SlidingExpiration); Assert.Equal(TimeSpan.FromHours(8), options.ExpireTimeSpan);
    }

    [Fact]
    public async Task EarlierExpiryIsRespectedRatherThanExtendedToEightHours()
    {
        await SignIn(15); _clock.Advance(TimeSpan.FromMinutes(16));
        Assert.Equal(HttpStatusCode.Unauthorized, (await _client.GetAsync("/protected")).StatusCode);
    }

    [Fact]
    public async Task SavedOidcAccessTokenExpiryCannotBeBypassedByALongerApplicationCookie()
    {
        var response = await _client.PostAsync("/fixture/login?minutes=480&tokenMinutes=5", null);
        _client.DefaultRequestHeaders.Add("Cookie", response.Headers.GetValues("Set-Cookie").Single(value => value.StartsWith(".AspNetCore.Cookies=")).Split(';')[0]);
        _clock.Advance(TimeSpan.FromMinutes(6));
        Assert.Equal(HttpStatusCode.Unauthorized, (await _client.GetAsync("/protected")).StatusCode);
    }

    [Fact]
    public async Task PreviouslyIssuedLongLivedTicketIsRejectedAfterAbsoluteCeiling()
    {
        var options = _app.Services.GetRequiredService<IOptionsMonitor<CookieAuthenticationOptions>>().Get("Cookies");
        var ticket = new AuthenticationTicket(new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub", "fixture")], "Fixture")),
            new AuthenticationProperties { IssuedUtc = _clock.GetUtcNow().AddHours(-9), ExpiresUtc = _clock.GetUtcNow().AddHours(5) }, "Cookies");
        _client.DefaultRequestHeaders.Add("Cookie", $"{options.Cookie.Name}={options.TicketDataFormat.Protect(ticket)}");
        Assert.Equal(HttpStatusCode.Unauthorized, (await _client.GetAsync("/protected")).StatusCode);
    }

    [Fact]
    public async Task LogoutPostRequiresCsrfAndClearsTheCookieUsingConfiguredOidcSignout()
    {
        await SignIn();
        Assert.Equal(HttpStatusCode.BadRequest, (await _client.PostAsync("/authentication/logout", null)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await _client.GetAsync("/protected")).StatusCode);
        var response = await _client.GetAsync("/authentication/antiforgery");
        var token = await response.Content.ReadFromJsonAsync<JsonElement>();
        var cookie = response.Headers.GetValues("Set-Cookie").Single(value => value.StartsWith(".AspNetCore.Antiforgery."));
        var sessionCookie = _client.DefaultRequestHeaders.GetValues("Cookie").Single();
        _client.DefaultRequestHeaders.Remove("Cookie");
        _client.DefaultRequestHeaders.Add("Cookie", sessionCookie + "; " + cookie.Split(';')[0]);
        _client.DefaultRequestHeaders.Add(token.GetProperty("headerName").GetString()!, token.GetProperty("requestToken").GetString());
        var logout = await _client.PostAsync("/authentication/logout", null);
        Assert.Equal(HttpStatusCode.Redirect, logout.StatusCode);
        Assert.StartsWith("https://identity.example.test/logout", logout.Headers.Location!.AbsoluteUri);
        Assert.Contains(logout.Headers.GetValues("Set-Cookie"), value => value.StartsWith(".AspNetCore.Cookies=;") && value.Contains("expires="));
    }

    [Theory]
    [InlineData("https://attacker.example", "/app")]
    [InlineData("//attacker.example", "/app")]
    [InlineData("/\\attacker.example", "/app")]
    [InlineData("/app\r\nheader", "/app")]
    [InlineData("/app/profile", "/app/profile")]
    public void LoginReturnTargetMustBeALocalPath(string input, string expected) => Assert.Equal(expected, AuthenticationEndpoints.SafeReturn(input));

    public async Task DisposeAsync() { _client.Dispose(); await _app.DisposeAsync(); }
    private sealed class ControlledClock : TimeProvider
    {
        private DateTimeOffset _now = DateTimeOffset.UtcNow;
        public override DateTimeOffset GetUtcNow() => _now;
        public void Advance(TimeSpan duration) => _now += duration;
    }
}
