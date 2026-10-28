using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using RBBH.ConnectedParties.IoC.Extensions.Health;
using RBBH.ConnectedParties.IoC.Extensions.Security;

namespace IntegrationTests.Tests.Operations;

public sealed class ProductionHostingHttpTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task AnonymousHealthKeepsProcessLiveDuringDependencyOutageWithoutLeakingDetails(bool dependencyAvailable)
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = "Production" });
        builder.WebHost.UseTestServer();
        builder.Services.AddAuthentication();
        builder.Services.AddAuthorization(options => options.FallbackPolicy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());
        builder.Services.AddSelftHealthCheck();
        builder.Services.AddHealthChecks().AddCheck("critical", () => dependencyAvailable
            ? HealthCheckResult.Healthy("internal-only dependency detail")
            : HealthCheckResult.Unhealthy("internal-only dependency detail", new Exception("internal-only exception")), tags: ["ready"]);
        await using var app = builder.Build();
        app.UseAuthentication(); app.UseAuthorization();
        app.MapCustomHealthChecks();
        await app.StartAsync();
        using var client = app.GetTestClient();
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/health/live")).StatusCode);
        var ready = await client.GetAsync("/health/ready");
        Assert.Equal(dependencyAvailable ? HttpStatusCode.OK : HttpStatusCode.ServiceUnavailable, ready.StatusCode);
        var json = await ready.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Single(json.EnumerateObject());
        Assert.Equal(dependencyAvailable ? "Healthy" : "Unhealthy", json.GetProperty("status").GetString());
        Assert.DoesNotContain("internal-only", await ready.Content.ReadAsStringAsync());
    }

    [Theory]
    [InlineData("10.20.30.40", "public.example.test", "https", "203.0.113.9", "public.example.test")]
    [InlineData("10.20.30.41", "public.example.test", "http", "10.20.30.41", "public.example.test")]
    // The trusted peer can forward address/proto; an unapproved host alone is ignored.
    [InlineData("10.20.30.40", "attacker.example.test", "https", "203.0.113.9", "public.example.test")]
    public async Task OnlyApprovedProxyAndPublicHostCanRewriteSchemeAndAuditAddress(
        string remote, string forwardedHost, string scheme, string address, string host)
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = "Production" });
        builder.WebHost.UseTestServer();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ReverseProxy:KnownProxies:0"] = "10.20.30.40",
            ["ReverseProxy:ForwardLimit"] = "1",
            ["AllowedHosts"] = "public.example.test"
        });
        builder.Services.AddApplicationForwardedHeaders(builder.Configuration, builder.Environment);
        await using var app = builder.Build();
        app.Use(async (context, next) => { context.Connection.RemoteIpAddress = IPAddress.Parse(remote); await next(); });
        app.UseForwardedHeaders();
        app.MapGet("/context", (Microsoft.AspNetCore.Http.HttpContext context) => new
        {
            scheme = context.Request.Scheme,
            address = context.Connection.RemoteIpAddress?.ToString(),
            host = context.Request.Host.Host
        });
        await app.StartAsync();
        using var client = app.GetTestClient();
        client.BaseAddress = new Uri("http://public.example.test");
        client.DefaultRequestHeaders.Add("X-Forwarded-For", "203.0.113.9");
        client.DefaultRequestHeaders.Add("X-Forwarded-Proto", "https");
        client.DefaultRequestHeaders.Add("X-Forwarded-Host", forwardedHost);
        var json = await client.GetFromJsonAsync<JsonElement>("/context");
        Assert.Equal(scheme, json.GetProperty("scheme").GetString());
        Assert.Equal(address, json.GetProperty("address").GetString());
        Assert.Equal(host, json.GetProperty("host").GetString());
    }
}
