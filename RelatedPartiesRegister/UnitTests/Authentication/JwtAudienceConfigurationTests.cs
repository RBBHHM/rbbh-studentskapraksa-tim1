using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using NSubstitute;
using RBBH.ConnectedParties.IoC.Extensions.Authentication;

namespace UnitTests.Authentication;

public sealed class JwtAudienceConfigurationTests
{
    private const string Issuer = "https://id.example.test/realms/rbbh";
    private const string Audience = "related-parties-api";

    private static ServiceProvider Build(string environmentName, string? audience)
    {
        var values = new Dictionary<string, string?>
        {
            ["KeycloakSettings:Enabled"] = "true",
            ["KeycloakSettings:Issuer"] = Issuer,
            ["KeycloakSettings:ClientId"] = "client",
            ["KeycloakSettings:ClientSecret"] = "not-a-real-secret",
            ["KeycloakSettings:Audience"] = audience
        };
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(values).Build();
        var environment = Substitute.For<IWebHostEnvironment>();
        environment.EnvironmentName.Returns(environmentName);
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddAuthenticationExtension(configuration, environment);
        return services.BuildServiceProvider();
    }

    [Theory]
    [InlineData("Production")]
    [InlineData("Staging")]
    [InlineData("UAT")]
    [InlineData("")]
    public void MissingAudienceOutsideDevelopmentFailsStartup(string environmentName)
    {
        foreach (var audience in new string?[] { null, "", "  " })
        {
            var error = Assert.Throws<InvalidOperationException>(() => Build(environmentName, audience));
            Assert.Contains("KeycloakSettings:Audience", error.Message);
        }
    }

    [Theory]
    [InlineData("Production")]
    [InlineData("Staging")]
    [InlineData("Development")]
    public void ConfiguredAudienceStartsInEveryEnvironmentAndIsValidated(string environmentName)
    {
        using var provider = Build(environmentName, Audience);
        var parameters = provider.GetRequiredService<IOptionsMonitor<JwtBearerOptions>>()
            .Get(JwtBearerDefaults.AuthenticationScheme).TokenValidationParameters;
        Assert.True(parameters.ValidateAudience);
        Assert.Equal(Audience, parameters.ValidAudience);
    }

    [Fact]
    public void DevelopmentWithoutAudienceKeepsDocumentedLocalBehavior()
    {
        using var provider = Build("Development", null);
        var parameters = provider.GetRequiredService<IOptionsMonitor<JwtBearerOptions>>()
            .Get(JwtBearerDefaults.AuthenticationScheme).TokenValidationParameters;
        Assert.False(parameters.ValidateAudience);
    }

    [Theory]
    [InlineData(Audience, true)]
    [InlineData("another-client-in-the-same-realm", false)]
    [InlineData("", false)]
    public async Task ConfiguredValidationParametersAcceptOnlyTheConfiguredAudience(string tokenAudience, bool accepted)
    {
        using var provider = Build("Production", Audience);
        var parameters = provider.GetRequiredService<IOptionsMonitor<JwtBearerOptions>>()
            .Get(JwtBearerDefaults.AuthenticationScheme).TokenValidationParameters.Clone();
        var key = new SymmetricSecurityKey(new byte[32].Select((_, index) => (byte)(index + 1)).ToArray());
        parameters.IssuerSigningKey = key;

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = Issuer,
            Expires = DateTime.UtcNow.AddMinutes(5),
            SigningCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256),
            Subject = new ClaimsIdentity([new Claim("sub", "subject")])
        };
        if (tokenAudience.Length > 0) descriptor.Audience = tokenAudience;
        var token = new JsonWebTokenHandler().CreateToken(descriptor);

        var result = await new JsonWebTokenHandler().ValidateTokenAsync(token, parameters);

        Assert.Equal(accepted, result.IsValid);
    }
}
