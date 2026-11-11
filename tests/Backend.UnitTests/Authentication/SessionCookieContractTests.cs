using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;
using RBBH.ConnectedParties.IoC.Extensions.Authentication;

namespace UnitTests.Authentication;

/// <summary>
/// Keeps the backend cookie contract aligned with the required absolute
/// eight-hour session; it must not silently become a sliding session.
/// </summary>
public sealed class SessionCookieContractTests
{
    [Theory]
    [InlineData("Production")]
    [InlineData("UAT")]
    public void ConfiguredKeycloakSessionUsesNonSlidingEightHourSecureCookie(string environmentName)
    {
        var settings = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["KeycloakSettings:Enabled"] = "true",
            ["KeycloakSettings:Issuer"] = "https://id.example.test/realms/fixture",
            ["KeycloakSettings:ClientId"] = "fixture",
            ["KeycloakSettings:ClientSecret"] = "synthetic-test-value",
            ["KeycloakSettings:Audience"] = "related-parties-api"
        }).Build();

        var environment = Mock.Of<IWebHostEnvironment>(item => item.EnvironmentName == environmentName);
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddAuthenticationExtension(settings, environment);

        using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptionsMonitor<CookieAuthenticationOptions>>()
            .Get(CookieAuthenticationDefaults.AuthenticationScheme);

        Assert.Equal(TimeSpan.FromHours(8), options.ExpireTimeSpan);
        Assert.False(options.SlidingExpiration);
        Assert.True(options.Cookie.HttpOnly);
        Assert.Equal(CookieSecurePolicy.Always, options.Cookie.SecurePolicy);
        Assert.Equal(SameSiteMode.Lax, options.Cookie.SameSite);
        Assert.NotNull(options.Events.OnSigningIn);
        Assert.NotNull(options.Events.OnValidatePrincipal);
    }
}
