using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Moq;
using RBBH.ConnectedParties.IoC.Extensions.Authentication;
using RBBH.ConnectedParties.IoC.Extensions.Databases;
using RBBH.ConnectedParties.IoC.Extensions.Security;

namespace UnitTests.Authentication;

public sealed class ProductionStartupTests
{
    [Fact]
    public void ProductionHttpAuthorityIsRejectedByHostStartupValidation()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        var environment = Environment("Production");
        services.AddSingleton(environment); services.AddSingleton<IHostEnvironment>(environment);
        services.AddAuthenticationExtension(Config(new Dictionary<string, string?>
        {
            ["KeycloakSettings:Enabled"] = "true", ["KeycloakSettings:Issuer"] = "http://id.example.test/realms/fixture",
            ["KeycloakSettings:ClientId"] = "fixture", ["KeycloakSettings:ClientSecret"] = "synthetic-not-a-credential",
            ["KeycloakSettings:Audience"] = "fixture"
        }), environment);
        using var provider = services.BuildServiceProvider();
        Assert.Contains("HTTPS", Assert.Throws<InvalidOperationException>(() => provider.GetRequiredService<IStartupValidator>().Validate()).Message);
    }

    private static IConfiguration Config(Dictionary<string, string?>? values = null) =>
        new ConfigurationBuilder().AddInMemoryCollection(values ?? []).Build();
    private static IWebHostEnvironment Environment(string name) => Mock.Of<IWebHostEnvironment>(item => item.EnvironmentName == name);

    [Theory]
    [InlineData("Production")]
    [InlineData("UAT")]
    public void MissingAuthenticationAndDatabaseAreRejectedBeforeServingTraffic(string name)
    {
        var environment = Environment(name);
        Assert.Throws<InvalidOperationException>(() => new ServiceCollection().AddAuthenticationExtension(Config(), environment));
        Assert.Throws<InvalidOperationException>(() => new ServiceCollection().AddDatabaseExtension(Config(), environment));
        Assert.Throws<InvalidOperationException>(() => new ServiceCollection().AddApplicationForwardedHeaders(Config(), environment));
    }

    [Theory]
    [InlineData("ReverseProxy:KnownNetworks:0", "0.0.0.0/0")]
    [InlineData("ReverseProxy:KnownProxies:0", "not-an-ip")]
    [InlineData("ReverseProxy:ForwardLimit", "0")]
    [InlineData("AllowedHosts", "*")]
    public void UnsafeProxyInputsHaveNoTrustAllFallback(string key, string value)
    {
        var settings = new Dictionary<string, string?>
        {
            ["ReverseProxy:KnownProxies:0"] = "10.20.30.40",
            ["ReverseProxy:ForwardLimit"] = "1", ["AllowedHosts"] = "public.example.test", [key] = value
        };
        Assert.Throws<InvalidOperationException>(() => new ServiceCollection().AddApplicationForwardedHeaders(Config(settings), Environment("Production")));
    }

    [Fact]
    public void UnconfiguredDevelopmentKeepsFrameworkLoopbackTrust()
    {
        var services = new ServiceCollection();
        services.AddApplicationForwardedHeaders(Config(), Environment("Development"));
        using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<Microsoft.AspNetCore.Builder.ForwardedHeadersOptions>>().Value;
        Assert.NotEmpty(options.KnownIPNetworks);
        Assert.NotEmpty(options.KnownProxies);
        Assert.Equal(1, options.ForwardLimit);
    }
}
