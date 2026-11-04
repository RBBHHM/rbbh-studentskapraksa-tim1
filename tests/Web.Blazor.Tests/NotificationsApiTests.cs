using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RBI.ApiCore;
using RBBH.RelatedPartiesRegister.Web.Api;
using RBBH.RelatedPartiesRegister.Web.Components;

namespace Web.Blazor.Tests;

public sealed class NotificationsApiTests
{
    private static ServiceProvider Client(Func<HttpRequestMessage, HttpResponseMessage> respond)
    {
        var services = new ServiceCollection(); services.AddLogging();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
        services.AddRbiApiClient<INotificationsApi, NotificationsApi>("Notifications", options => options.BaseUrl = new("https://api.example.test/"))
            .ConfigurePrimaryHttpMessageHandler(() => new Handler(respond));
        return services.BuildServiceProvider();
    }
    private static HttpResponseMessage Json(HttpStatusCode status, string body) => new(status) { Content = new StringContent(body, Encoding.UTF8, "application/json") };

    [Fact]
    public async Task InboxReadsTheRecipientScopedEnvelopeWithUnreadCount()
    {
        string? query = null;
        await using var provider = Client(request =>
        {
            query = request.RequestUri!.PathAndQuery;
            return Json(HttpStatusCode.OK, "{\"items\":[{\"id\":\"11111111-1111-1111-1111-111111111111\",\"titleKey\":\"notifications.role.approved.title\",\"bodyKey\":\"notifications.role.approved.body\",\"parameters\":{\"roles\":\"FL\"},\"route\":\"/app/profile\",\"createdAt\":\"2026-11-03T10:00:00Z\",\"readAt\":null}],\"total\":1,\"page\":2,\"pageSize\":20,\"unreadCount\":1}");
        });
        var page = await provider.GetRequiredService<INotificationsApi>().InboxAsync(2, 20, true);
        Assert.Equal("/api/notifications?page=2&pageSize=20&unreadOnly=true", query);
        Assert.Equal(1, page.UnreadCount); var item = Assert.Single(page.Items);
        Assert.True(item.IsUnread); Assert.Equal("/app/profile", item.Route); Assert.Equal("FL", item.Parameters.GetProperty("roles").GetString());
    }
    [Fact]
    public async Task MarkReadPostsToTheItemAndAcceptsNoContent()
    {
        HttpMethod? method = null; string? path = null;
        await using var provider = Client(request => { method = request.Method; path = request.RequestUri!.AbsolutePath; return new HttpResponseMessage(HttpStatusCode.NoContent); });
        var id = Guid.NewGuid(); await provider.GetRequiredService<INotificationsApi>().MarkReadAsync(id);
        Assert.Equal(HttpMethod.Post, method); Assert.Equal($"/api/notifications/{id}/read", path);
    }
    [Theory]
    [InlineData(403, "errors.forbidden")]
    [InlineData(404, "errors.unavailable")]
    [InlineData(503, "errors.unavailable")]
    public async Task ReadErrorsKeepOwnedCopy(int status, string key)
    {
        await using var provider = Client(_ => Json((HttpStatusCode)status, "{\"detail\":\"private-marker\"}"));
        var error = await Assert.ThrowsAsync<RegistryApiException>(() => provider.GetRequiredService<INotificationsApi>().InboxAsync());
        Assert.Equal(key, error.MessageKey); Assert.DoesNotContain("private-marker", error.ToString());
    }
    [Fact]
    public void NamedPlaceholdersAreFilledFromNotificationParametersAndUnknownOnesStayVisible()
    {
        using var parameters = JsonDocument.Parse("{\"role\":\"FL\",\"count\":2}");
        Assert.Equal("Role FL granted, 2 total, {missing}", RuntimeResources.Fill("Role {role} granted, {count} total, {missing}", parameters.RootElement));
        Assert.Equal("plain", RuntimeResources.Fill("plain", default));
    }
    private sealed class Handler(Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token) => Task.FromResult(respond(request));
    }
}
