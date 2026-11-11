using System.Net;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RBI.ApiCore;
using RBBH.RelatedPartiesRegister.Web.Api;

namespace Web.Blazor.Tests;

public sealed class ReleaseNotesApiTests
{
    private sealed class Handler(Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct) =>
            Task.FromResult(respond(request));
    }

    private static ServiceProvider Client(Func<HttpRequestMessage, HttpResponseMessage> respond)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
        services.AddRbiApiClient<IReleaseNotesApi, ReleaseNotesApi>("ReleaseNotes",
            options => options.BaseUrl = new("https://api.example.test/"))
            .ConfigurePrimaryHttpMessageHandler(() => new Handler(respond));
        return services.BuildServiceProvider();
    }

    [Theory]
    [InlineData("uat")]
    [InlineData("prod")]
    public async Task ReadsOnlyTheSelectedReleaseEnvironment(string environment)
    {
        string? requestPath = null;
        await using var provider = Client(request =>
        {
            requestPath = request.RequestUri!.PathAndQuery;
            return new(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"items\":[{\"tag\":\"v1.0.0\",\"title\":\"Test\",\"body\":\"Notes\",\"publishedAt\":null,\"url\":\"\",\"environment\":\"prod\"}]}", Encoding.UTF8, "application/json")
            };
        });
        var entries = await provider.GetRequiredService<IReleaseNotesApi>().ListAsync(environment);
        Assert.Single(entries.Items);
        Assert.Equal($"/api/release-notes?environment={environment}", requestPath);
    }

    [Fact]
    public async Task UnavailableGitHubNeverInventsReleaseHistory()
    {
        await using var provider = Client(_ => new(HttpStatusCode.ServiceUnavailable));
        await Assert.ThrowsAsync<RegistryApiException>(() => provider.GetRequiredService<IReleaseNotesApi>().ListAsync());
    }

    [Fact]
    public async Task InvalidEnvironmentNeverReachesNetwork()
    {
        await using var provider = Client(_ => throw new InvalidOperationException("Network must not be called"));
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => provider.GetRequiredService<IReleaseNotesApi>().ListAsync("other"));
    }
}
