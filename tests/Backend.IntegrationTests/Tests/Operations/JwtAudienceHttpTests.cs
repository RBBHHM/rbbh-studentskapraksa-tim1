using System.Net;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using RBBH.ConnectedParties.DL.Persistence;
using RBBH.ConnectedParties.IoC.Extensions.Authentication;

namespace IntegrationTests.Tests.Operations;

public sealed class JwtAudienceHttpTests
{
    // Test-only issuer/key. No external realm, secrets or network is used.
    private const string Issuer = "https://identity.example.test/realms/fixture";
    private const string Audience = "fixture-api";

    public static TheoryData<object?, bool> Audiences => new()
    {
        { Audience, true },
        { "wrong-api", false },
        { null, false },
        { "", false },
        { "   ", false },
        { 123, false },
        { Array.Empty<string>(), false },
        { new[] { "other-api", "third-api" }, false },
        { new[] { "other-api", Audience }, true }
    };

    [Theory]
    [MemberData(nameof(Audiences))]
    public async Task RealBearerPipelineAcceptsOnlyAnExplicitMatchingAudience(object? audience, bool accepted)
    {
        var key = new SymmetricSecurityKey(Enumerable.Range(1, 32).Select(value => (byte)value).ToArray());
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = "Production" });
        builder.WebHost.UseTestServer();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["KeycloakSettings:Enabled"] = "true",
            ["KeycloakSettings:Issuer"] = Issuer,
            ["KeycloakSettings:ClientId"] = "fixture-oidc-client",
            ["KeycloakSettings:ClientSecret"] = "test-placeholder",
            ["KeycloakSettings:Audience"] = Audience
        });
        builder.Services.AddDbContext<ConnectedPartiesDbContext>(options => options.UseInMemoryDatabase(Guid.NewGuid().ToString()));
        builder.Services.AddAuthenticationExtension(builder.Configuration, builder.Environment);
        builder.Services.PostConfigure<JwtBearerOptions>("Bearer", options =>
        {
            var metadata = new OpenIdConnectConfiguration { Issuer = Issuer };
            metadata.SigningKeys.Add(key);
            options.ConfigurationManager = new StaticConfigurationManager<OpenIdConnectConfiguration>(metadata);
        });
        await using var app = builder.Build();
        app.UseAuthentication();
        app.UseAuthorization();
        var actionCalls = 0;
        app.MapGet("/protected", () => { actionCalls++; return "ok"; }).RequireAuthorization();
        await app.StartAsync();
        using var client = app.GetTestClient();
        var claims = new Dictionary<string, object> { ["sub"] = "fixture-subject" };
        if (audience is not null) claims["aud"] = audience;
        var token = new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
        {
            Issuer = Issuer,
            Claims = claims,
            Expires = DateTime.UtcNow.AddMinutes(5),
            SigningCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256)
        });
        client.DefaultRequestHeaders.Authorization = new("Bearer", token);
        var response = await client.GetAsync("/protected");
        Assert.Equal(accepted ? HttpStatusCode.OK : HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal(accepted ? 1 : 0, actionCalls);

        // Configuring API audience must not replace OIDC's client identity or flow.
        var oidc = app.Services.GetRequiredService<IOptionsMonitor<OpenIdConnectOptions>>().Get("KeycloakOidc");
        Assert.Equal("fixture-oidc-client", oidc.ClientId);
        Assert.Equal("code", oidc.ResponseType);
        Assert.True(oidc.UsePkce);
        Assert.True(oidc.TokenValidationParameters.ValidateAudience);
    }
}
