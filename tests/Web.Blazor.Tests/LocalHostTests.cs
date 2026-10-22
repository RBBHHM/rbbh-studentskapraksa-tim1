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
            while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "RelatedPartiesRegister.Migration.slnx")))
                directory = directory.Parent;
            if (directory is null) throw new InvalidOperationException("Migration solution not found.");
            builder.UseContentRoot(Path.Combine(directory.FullName, "src", "Web.Blazor"));
            builder.UseEnvironment("Development");
            builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["Apis:BaseUrl"] = "", ["Authentication:Authority"] = "",
                    ["Authentication:ClientId"] = "", ["Authentication:ClientSecret"] = ""
                }));
        }
    }

    [Theory]
    [InlineData("/app")]
    [InlineData("/app/profile")]
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
