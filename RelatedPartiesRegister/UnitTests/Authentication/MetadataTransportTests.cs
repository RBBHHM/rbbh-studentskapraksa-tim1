using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Moq;
using RBBH.ConnectedParties.IoC.Extensions.Authentication;

namespace UnitTests.Authentication;

public sealed class MetadataTransportTests
{
    [Theory]
    [InlineData("Production", "http://localhost:8180/realms/fixture", true)]
    [InlineData("Staging", "http://localhost:8180/realms/fixture", true)]
    [InlineData("UAT", "http://localhost:8180/realms/fixture", true)]
    [InlineData("Development", "http://localhost.attacker.test/realms/fixture", true)]
    [InlineData("Development", "http://localhost:8180/realms/fixture", false)]
    [InlineData("Development", "https://identity.example.test/realms/fixture", true)]
    public void OnlyActualDevelopmentLoopbackCanDisableHttpsMetadata(string environmentName, string issuer, bool required)
    {
        var services = new ServiceCollection(); services.AddLogging();
        var environment = Mock.Of<IWebHostEnvironment>(item => item.EnvironmentName == environmentName);
        services.AddSingleton(environment); services.AddSingleton<IHostEnvironment>(environment);
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["KeycloakSettings:Issuer"] = issuer,
            ["KeycloakSettings:Audience"] = "fixture-audience",
            ["KeycloakSettings:ClientId"] = "fixture-client",
            ["KeycloakSettings:ClientSecret"] = "synthetic-test-value-not-a-credential"
        }).Build();
        services.AddAuthenticationExtension(config, environment);
        using var provider = services.BuildServiceProvider();
        if (required && new Uri(issuer).Scheme == Uri.UriSchemeHttp)
        {
            // Framework post-configuration actively rejects HTTP authority when
            // RequireHttpsMetadata is true, before any network request occurs.
            Assert.Contains("HTTPS", Assert.Throws<InvalidOperationException>(() => provider.GetRequiredService<IOptionsMonitor<JwtBearerOptions>>().Get(JwtBearerDefaults.AuthenticationScheme)).Message);
            Assert.Contains("HTTPS", Assert.Throws<InvalidOperationException>(() => provider.GetRequiredService<IOptionsMonitor<OpenIdConnectOptions>>().Get("KeycloakOidc")).Message);
            return;
        }
        Assert.Equal(required, provider.GetRequiredService<IOptionsMonitor<JwtBearerOptions>>().Get(JwtBearerDefaults.AuthenticationScheme).RequireHttpsMetadata);
        Assert.Equal(required, provider.GetRequiredService<IOptionsMonitor<OpenIdConnectOptions>>().Get("KeycloakOidc").RequireHttpsMetadata);
    }
}
