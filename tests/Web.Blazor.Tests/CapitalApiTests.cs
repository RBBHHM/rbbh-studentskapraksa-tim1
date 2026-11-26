using System.Net;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RBI.ApiCore;
using RBBH.RelatedPartiesRegister.Web.Api;

namespace Web.Blazor.Tests;

public sealed class CapitalApiTests
{
    private static ServiceProvider Client(HttpStatusCode status, string? body)
    {
        var services = new ServiceCollection(); services.AddLogging();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
        services.AddRbiApiClient<ICapitalApi, CapitalApi>("Capital", options => options.BaseUrl = new("https://api.example.test/"))
            .ConfigurePrimaryHttpMessageHandler(() => new Handler(status, body));
        return services.BuildServiceProvider();
    }
    [Theory]
    [InlineData(204, null)]
    [InlineData(200, "null")]
    public async Task OptionalSingletonIsEmptyNotUnavailableOrFictionalZeros(int status, string? body)
    {
        await using var provider = Client((HttpStatusCode)status, body);
        Assert.Null(await provider.GetRequiredService<ICapitalApi>().GetAsync());
    }
    [Theory]
    [InlineData(200, "{}")]
    [InlineData(200, "[]")]
    [InlineData(200, "malformed")]
    [InlineData(200, "{\"regulatoryCapital\":0}")]
    [InlineData(404, "{\"detail\":\"private-marker\"}")]
    [InlineData(503, "{\"detail\":\"private-marker\"}")]
    public async Task MalformedMissingOrFailedResponsesNeverBecomeAValidEmptyState(int status, string body)
    {
        await using var provider = Client((HttpStatusCode)status, body);
        var error = await Assert.ThrowsAsync<RegistryApiException>(() => provider.GetRequiredService<ICapitalApi>().GetAsync());
        Assert.DoesNotContain("private-marker", error.ToString());
    }
    [Fact]
    public async Task RealZeroValuesArePreserved()
    {
        const long id = 3;
        await using var provider = Client(HttpStatusCode.OK, $"{{\"id\":{id},\"regulatoryCapital\":0,\"coreCapital\":0,\"supplementaryCapital\":0,\"capitalDate\":\"2026-01-01T00:00:00\"}}");
        var record = await provider.GetRequiredService<ICapitalApi>().GetAsync();
        Assert.NotNull(record); Assert.Equal(id, record.Id); Assert.Equal(0m, record.CoreCapital);
    }
    [Fact]
    public async Task InvalidCapitalEndDateReturnsTheResourceKeyWithoutThrowing()
    {
        const string problem = """
            {"code":"capital.valid_to_past","detail":"internal details"}
            """;
        await using var provider = Client(HttpStatusCode.BadRequest, problem);
        var result = await provider.GetRequiredService<ICapitalApi>().SaveAsync(new(60, 50, 40, DateTime.UtcNow.Date.AddDays(-1)));
        Assert.Equal("capital.valid-to-past", result.ErrorKey);
        Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);
    }

    private sealed class Handler(HttpStatusCode status, string? body) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
        {
            Assert.Equal("/api/capital", request.RequestUri!.AbsolutePath);
            return Task.FromResult(new HttpResponseMessage(status) { Content = body is null ? null : new StringContent(body, Encoding.UTF8, "application/json") });
        }
    }
}
