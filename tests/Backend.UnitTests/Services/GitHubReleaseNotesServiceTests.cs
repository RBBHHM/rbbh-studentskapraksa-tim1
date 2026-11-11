using System.Net;
using System.Text;
using Microsoft.Extensions.Caching.Memory;
using RBBH.ConnectedParties.BL.Services;
using RBBH.ConnectedParties.Exceptions;

namespace UnitTests.Services;

public sealed class GitHubReleaseNotesServiceTests
{
    private sealed class Handler(Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct) =>
            Task.FromResult(respond(request));
    }

    private static GitHubReleaseNotesService Create(Func<HttpRequestMessage, HttpResponseMessage> respond) =>
        new(new HttpClient(new Handler(respond)), new MemoryCache(new MemoryCacheOptions()));

    private const string Example = """
        [
          {"tag_name":"uat-1.2.0","name":"UAT test","body":"Latest pre-release","published_at":"2026-11-11T10:00:00Z","html_url":"https://github.com/RBBHHM/rbbh-studentskapraksa-tim1/releases/tag/uat-1.2.0","prerelease":true,"draft":false},
          {"tag_name":"v1.1.0","name":"PROD release","body":"Production notes","published_at":"2026-11-01T10:00:00Z","html_url":"https://github.com/RBBHHM/rbbh-studentskapraksa-tim1/releases/tag/v1.1.0","prerelease":false,"draft":false},
          {"tag_name":"v0.1.0","name":"Draft","body":"Should not leak","prerelease":false,"draft":true}
        ]
        """;

    [Fact]
    public async Task SeparatesUatAndProductionReleasesAndCachesSource()
    {
        var calls = 0;
        var service = Create(request =>
        {
            calls++;
            Assert.Equal("api.github.com", request.RequestUri!.Host);
            Assert.Equal(HttpMethod.Get, request.Method);
            Assert.Contains("RBBH-RelatedPartiesRegister", request.Headers.UserAgent.ToString());
            return new(HttpStatusCode.OK) { Content = new StringContent(Example, Encoding.UTF8, "application/json") };
        });

        var uat = await service.GetAsync("uat", default);
        var prod = await service.GetAsync("prod", default);
        Assert.Equal("uat-1.2.0", Assert.Single(uat.Items).Tag);
        Assert.Equal("v1.1.0", Assert.Single(prod.Items).Tag);
        Assert.Equal("Production notes", Assert.Single(prod.Items).Body);
        Assert.Equal(1, calls);
    }

    [Fact]
    public async Task DisallowsUnapprovedReleaseLinksAndNeverRendersThirdPartyHtml()
    {
        var json = "[{\"tag_name\":\"v1.0.0\",\"name\":\"v1\",\"body\":\"<script>bad()</script>\",\"html_url\":\"https://untrusted.example/unsafe\",\"prerelease\":false}]";
        var service = Create(_ => new(HttpStatusCode.OK) { Content = new StringContent(json, Encoding.UTF8, "application/json") });
        var release = Assert.Single((await service.GetAsync("prod", default)).Items);
        Assert.Empty(release.Url);
        Assert.Equal("<script>bad()</script>", release.Body); // Razor must HTML-encode the body, never use MarkupString.
    }

    [Theory]
    [InlineData(HttpStatusCode.Forbidden)]
    [InlineData(HttpStatusCode.NotFound)]
    [InlineData(HttpStatusCode.TooManyRequests)]
    public async Task ApiFailuresFailClosed(HttpStatusCode status)
    {
        var service = Create(_ => new(status) { Content = new StringContent("no access") });
        await Assert.ThrowsAsync<BusinessRuleException>(() => service.GetAsync("uat", default));
    }

    [Fact]
    public async Task EmptyReleasesAreEmptyRatherThanFakeVersion()
    {
        var service = Create(_ => new(HttpStatusCode.OK) { Content = new StringContent("[]") });
        Assert.Empty((await service.GetAsync("uat", default)).Items);
        Assert.Empty((await service.GetAsync("prod", default)).Items);
    }

    [Fact]
    public async Task RejectsUnknownEnvironment()
    {
        var service = Create(_ => throw new InvalidOperationException("Should not call external service"));
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => service.GetAsync("production", default));
    }
}
