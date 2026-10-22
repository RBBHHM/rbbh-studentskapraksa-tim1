using System.Net;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RBI.ApiCore;
using RBBH.RelatedPartiesRegister.Web.Api;

namespace Web.Blazor.Tests;

public sealed class AuditApiTests
{
    [Theory]
    [InlineData("admin", true)]
    [InlineData("Admin", false)]
    [InlineData("gost", false)]
    public void AdminOnlyMatchesActualBackendApplicationRoleNotPermissionOrProviderRole(string role, bool expected)
    {
        var profile = new ApplicationProfile("u", "User", "user", "fixture@example.test", [role], ["ADMINISTRATION_MANAGE"]);
        Assert.Equal(expected, ApplicationAccess.IsAdministrator(profile));
    }
    [Fact]
    public async Task ActualAuditContractUsesLogsAndSourcePageSizeNotStarterAuditEnvelope()
    {
        await using var provider = Client(HttpStatusCode.OK, "{\"logs\":[],\"total\":201,\"page\":2,\"pageSize\":200}");
        var result = await provider.GetRequiredService<IAuditApi>().GetAsync(2);
        Assert.Empty(result.Logs); Assert.Equal(201, result.Total); Assert.Equal(2, result.Page);
    }
    [Fact]
    public void ScalarSearchIncludesHiddenResponseMetadataAndTrimsInput()
    {
        var entry = new AuditRecord(Guid.NewGuid(), "RelatedPerson", "record", "INSERT", "Create", "Individuals", "note", "old-value", "new-value", "auditor", "127.0.0.1", DateTime.UtcNow);
        Assert.True(entry.Matches("  OLD-VALUE  ")); Assert.True(entry.Matches("127.0.0.1")); Assert.False(entry.Matches("unmatched"));
    }
    [Fact]
    public async Task UnauthorizedAuditCannotBecomeAnEmptySuccessfulList()
    {
        await using var provider = Client(HttpStatusCode.Forbidden, "{\"detail\":\"private-marker\"}");
        var error = await Assert.ThrowsAsync<RegistryApiException>(() => provider.GetRequiredService<IAuditApi>().GetAsync(2));
        Assert.Equal("errors.forbidden", error.MessageKey); Assert.DoesNotContain("private-marker", error.ToString());
    }
    private static ServiceProvider Client(HttpStatusCode status, string body)
    {
        var services = new ServiceCollection(); services.AddLogging();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
        services.AddRbiApiClient<IAuditApi, AuditApi>("Audit", options => options.BaseUrl = new("https://api.example.test/"))
            .ConfigurePrimaryHttpMessageHandler(() => new Handler(status, body));
        return services.BuildServiceProvider();
    }
    private sealed class Handler(HttpStatusCode status, string body) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
        {
            Assert.Equal("/api/audit-logs?page=2&pageSize=200", request.RequestUri!.PathAndQuery);
            return Task.FromResult(new HttpResponseMessage(status) { Content = new StringContent(body, Encoding.UTF8, "application/json") });
        }
    }
}
