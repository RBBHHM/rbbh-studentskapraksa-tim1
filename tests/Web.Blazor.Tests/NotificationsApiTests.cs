using System.Net;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RBI.ApiCore;
using RBBH.RelatedPartiesRegister.Web.Api;

namespace Web.Blazor.Tests;

public sealed class NotificationsApiTests
{
    private static ServiceProvider Client(HttpStatusCode status, string body)
    {
        var services = new ServiceCollection(); services.AddLogging();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
        services.AddRbiApiClient<INotificationsApi, NotificationsApi>("Notifications", options => options.BaseUrl = new("https://api.example.test/"))
            .ConfigurePrimaryHttpMessageHandler(() => new Handler(status, body));
        return services.BuildServiceProvider();
    }
    [Fact]
    public async Task ExactProviderEmailsContractDoesNotInventSmtpDeliveryRecords()
    {
        await using var provider = Client(HttpStatusCode.OK, "{\"provider\":\"smtp\",\"emails\":[]}");
        var log = await provider.GetRequiredService<INotificationsApi>().GetAsync(); Assert.Equal("smtp", log.Provider); Assert.Empty(log.Emails);
    }
    [Fact]
    public void ScalarSearchRetainsRecordedMetadataAndBodyWithoutMarkupRendering()
    {
        var entry = new RecordedNotification(Guid.NewGuid(), "fixture@example.test", "Subject", "<img src=x>body-search-needle", DateTime.UtcNow, "broadcast", "Business notice", "Recorded", null);
        Assert.True(entry.Matches("BODY-SEARCH-NEEDLE")); Assert.True(entry.Matches("broadcast")); Assert.True(entry.Matches("fixture@")); Assert.False(entry.Matches("unmatched"));
        Assert.True(entry.Matches("  fixture@  "));
    }
    [Theory]
    [InlineData(403, "errors.forbidden")]
    [InlineData(503, "errors.unavailable")]
    public async Task ReadErrorsKeepOwnedCopy(int status, string key)
    {
        await using var provider = Client((HttpStatusCode)status, "{\"detail\":\"private-marker\"}");
        var error = await Assert.ThrowsAsync<RegistryApiException>(() => provider.GetRequiredService<INotificationsApi>().GetAsync());
        Assert.Equal(key, error.MessageKey); Assert.DoesNotContain("private-marker", error.ToString());
    }
    private sealed class Handler(HttpStatusCode status, string body) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
        {
            Assert.Equal(HttpMethod.Get, request.Method); Assert.Equal("/api/email-log", request.RequestUri!.AbsolutePath);
            return Task.FromResult(new HttpResponseMessage(status) { Content = new StringContent(body, Encoding.UTF8, "application/json") });
        }
    }
}
