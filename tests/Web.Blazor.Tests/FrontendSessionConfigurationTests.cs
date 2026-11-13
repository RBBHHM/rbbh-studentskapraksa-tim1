using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Moq;
using RBI.Authentication;

namespace Web.Blazor.Tests;

/// <summary>
/// Tests the actual optional frontend OIDC/cookie registration (the backend
/// session tests alone cannot prove the browser's absolute cookie contract).
/// No real identity provider, credentials, network or session state is used.
/// </summary>
public sealed class FrontendSessionConfigurationTests
{
    [Fact]
    public void FrontendSessionIsAnAbsoluteSecureEightHoursWithNoSlidingCookie()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Authentication:Authority"] = "https://login.example.test/realms/fixture",
            ["Authentication:ClientId"] = "fixture-client",
            ["Authentication:ClientSecret"] = "synthetic-unit-test-only"
        }).Build();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(Mock.Of<IHostEnvironment>(e => e.EnvironmentName == Environments.Production));
        services.AddDistributedMemoryCache();
        services.AddRbiAuthentication(configuration);

        using var provider = services.BuildServiceProvider();
        var cookie = provider.GetRequiredService<IOptionsMonitor<CookieAuthenticationOptions>>()
            .Get("RBI.Cookie");
        Assert.Equal(TimeSpan.FromHours(8), cookie.ExpireTimeSpan);
        Assert.False(cookie.SlidingExpiration);
        Assert.True(cookie.Cookie.HttpOnly);
        Assert.Equal(CookieSecurePolicy.Always, cookie.Cookie.SecurePolicy);
        Assert.Equal(SameSiteMode.Lax, cookie.Cookie.SameSite);
        Assert.Equal("__Host-rbi-session", cookie.Cookie.Name);
        Assert.Equal("/", cookie.Cookie.Path);
        Assert.Equal("RbiCookieAuthenticationEvents", cookie.EventsType?.Name);
        var oidc = provider.GetRequiredService<IOptionsMonitor<OpenIdConnectOptions>>()
            .Get("RBI.OpenIdConnect");
        Assert.True(oidc.UsePkce);
        Assert.Equal(TimeSpan.FromHours(8), oidc.MaxAge);
        Assert.False(oidc.SaveTokens);
        Assert.True(oidc.RequireHttpsMetadata);
    }
}
