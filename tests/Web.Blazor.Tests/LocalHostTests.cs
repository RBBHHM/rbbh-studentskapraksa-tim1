using System.Net;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace Web.Blazor.Tests;

public sealed class LocalHostTests
{
    private sealed class LocalApp : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);
            while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "RelatedPartiesRegister.slnx")))
                directory = directory.Parent;
            if (directory is null) throw new InvalidOperationException("Migration solution not found.");
            builder.UseContentRoot(Path.Combine(directory.FullName, "Frontend"));
            builder.UseEnvironment("Development");
            builder.UseSetting("Apis:BaseUrl", "");
            builder.UseSetting("Authentication:Authority", "");
            builder.UseSetting("Authentication:ClientId", "");
            builder.UseSetting("Authentication:ClientSecret", "");
        }
    }

    private sealed class ChallengeApp : WebApplicationFactory<Program>
    {
        internal OpenIdConnectOptions Oidc => Services.GetRequiredService<IOptionsMonitor<OpenIdConnectOptions>>()
            .Get("RBI.OpenIdConnect");

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseContentRoot(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "Frontend"));
            builder.UseEnvironment("Development");
            // UseSetting is visible to Program before the builder resolves identity configuration.
            builder.UseSetting("Apis:BaseUrl", "http://localhost:5000/");
            builder.UseSetting("Authentication:Authority", "https://identity.invalid/realms/test");
            builder.UseSetting("Authentication:ClientId", "test-client");
            builder.UseSetting("Authentication:ClientSecret", "test-secret");
            builder.ConfigureTestServices(services => services.PostConfigure<OpenIdConnectOptions>(
                "RBI.OpenIdConnect", options => options.ConfigurationManager =
                    new StaticConfigurationManager<OpenIdConnectConfiguration>(new()
                    {
                        Issuer = "https://identity.invalid/realms/test",
                        AuthorizationEndpoint = "https://identity.invalid/realms/test/protocol/openid-connect/auth"
                    })));
        }
    }

    [Theory]
    [InlineData("/")]
    [InlineData("/profile")]
    [InlineData("/app/legal-persons?search=test")]
    [InlineData("/app/admin/users")]
    public async Task ProtectedRouteChallengesKeycloakDirectlyAndPreservesReturnUrl(string route)
    {
        await using var app = new ChallengeApp();
        using var client = app.CreateClient(new() { BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false });
        var response = await client.GetAsync(route);
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        var target = response.Headers.Location!;
        Assert.Equal("identity.invalid", target.Host);
        Assert.Equal("/realms/test/protocol/openid-connect/auth", target.AbsolutePath);
        var query = QueryHelpers.ParseQuery(target.Query);
        Assert.Equal("test-client", query["client_id"].ToString());
        Assert.Equal(route, app.Oidc.StateDataFormat.Unprotect(query["state"].ToString())!.RedirectUri);
    }

    [Fact]
    public async Task FailedIdentityCallbackRedirectsToAnAnonymousStaticErrorPage()
    {
        await using var app = new ChallengeApp();
        using var client = app.CreateClient(new() { BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false });
        var challenge = await client.GetAsync("/app");
        var state = QueryHelpers.ParseQuery(challenge.Headers.Location!.Query)["state"].ToString();
        var failure = await client.GetAsync(QueryHelpers.AddQueryString("/signin-oidc", new Dictionary<string, string?>
        {
            ["error"] = "server_error",
            ["state"] = state
        }));
        Assert.Equal(HttpStatusCode.Redirect, failure.StatusCode);
        Assert.Equal("/error", failure.Headers.Location?.OriginalString);
        var errorPage = await client.GetAsync("/error");
        Assert.Equal(HttpStatusCode.OK, errorPage.StatusCode);
        var html = await errorPage.Content.ReadAsStringAsync();
        Assert.DoesNotContain("_framework/blazor.web.js", html);
        Assert.DoesNotContain("<!--Blazor:", html);
    }

    [Theory]
    [InlineData("/app")]
    [InlineData("/app/profile")]
    [InlineData("/app/physical-persons")]
    [InlineData("/app/legal-persons")]
    [InlineData("/app/limits")]
    [InlineData("/app/capital")]
    [InlineData("/app/reports")]
    [InlineData("/app/notifications")]
    [InlineData("/app/admin/audit")]
    [InlineData("/app/admin/users")]
    [InlineData("/app/admin/period")]
    [InlineData("/app/admin/code-lists")]
    [InlineData("/app/guide")]
    public async Task LocalUnconfiguredHostIsHonestAndSignedOut(string path)
    {
        await using var app = new LocalApp();
        using var client = app.CreateClient(new() { BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false });
        var response = await client.GetAsync(path);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var html = await response.Content.ReadAsStringAsync();
        Assert.Contains("lang=\"bs\"", html);
        Assert.Contains("Autentifikacija nije", WebUtility.HtmlDecode(html));
        Assert.DoesNotContain("admin@localhost", html);
        Assert.DoesNotContain("/authentication/login", html);
        Assert.Contains("<option value=\"de\">Deutsch</option>", html);
        Assert.Contains("RBI.DesignSystem/rbi-design-system.css", html);
        Assert.Contains("_framework/blazor.web.js", html);
    }

    [Fact]
    public async Task UnconfiguredModeDoesNotExposeLoginHandler()
    {
        await using var app = new LocalApp();
        using var client = app.CreateClient(new() { BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false });
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/authentication/login")).StatusCode);
    }
}
