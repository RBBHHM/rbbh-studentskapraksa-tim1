using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

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
            builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["Apis:BaseUrl"] = "",
                    ["Authentication:Authority"] = "",
                    ["Authentication:ClientId"] = "",
                    ["Authentication:ClientSecret"] = ""
                }));
        }
    }

    private sealed class ChallengeApp : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseContentRoot(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "Frontend"));
            builder.UseEnvironment("Development");
            // UseSetting is visible to Program before the builder resolves identity configuration.
            builder.UseSetting("Apis:BaseUrl", "http://localhost:5000/");
            builder.UseSetting("Authentication:Authority", "https://identity.invalid/realms/test");
            builder.UseSetting("Authentication:ClientId", "test-client");
            builder.UseSetting("Authentication:ClientSecret", "test-secret");
        }
    }

    [Fact]
    public async Task ProtectedRouteStartsTheIdentityChallengeWithoutSignInScreen()
    {
        await using var app = new ChallengeApp();
        using var client = app.CreateClient(new() { BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false });
        var response = await client.GetAsync("/app/legal-persons");
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        var target = response.Headers.Location!.OriginalString;
        Assert.StartsWith("/authentication/login?returnUrl=", target);
        Assert.Contains(Uri.EscapeDataString("/app/legal-persons"), target);
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
        Assert.DoesNotContain("Deutsch", html);
        Assert.Contains("RBI.DesignSystem/rbi-design-system.css", html);
    }

    [Fact]
    public async Task UnconfiguredModeDoesNotExposeLoginHandler()
    {
        await using var app = new LocalApp();
        using var client = app.CreateClient(new() { BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false });
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/authentication/login")).StatusCode);
    }
}
