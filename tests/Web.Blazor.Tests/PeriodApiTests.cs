using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RBI.ApiCore;
using RBBH.RelatedPartiesRegister.Web.Api;
using RBBH.RelatedPartiesRegister.Web.Components.Registry;

namespace Web.Blazor.Tests;

public sealed class PeriodApiTests
{
    [Theory]
    [InlineData(9, false)]
    [InlineData(10, true)]
    [InlineData(490, true)]
    [InlineData(491, false)]
    [InlineData(500, false)]
    public void ResponseDraftUsesApprovedBoundsWithoutTruncatingInput(int length, bool valid)
    {
        var draft = new PeriodNoteDraft { Kind = PeriodNoteKind.Response, Text = new string('x', length) };
        Assert.Equal(490, draft.Maximum); Assert.Equal(valid, draft.ErrorKey is null);
        Assert.Equal(length, draft.Text.Length);
    }
    [Theory]
    [InlineData(PeriodNoteKind.RequestUnlock)]
    [InlineData(PeriodNoteKind.Reject)]
    [InlineData(PeriodNoteKind.RequestInfo)]
    public void ResponseLimitDoesNotChangeOtherExistingNoteLimits(PeriodNoteKind kind)
    {
        var draft = new PeriodNoteDraft { Kind = kind, Text = new string('x', 500) };
        Assert.Equal(500, draft.Maximum); Assert.Null(draft.ErrorKey);
    }
    [Fact]
    public void WhitespaceCannotSatisfyTheMeaningfulMinimum()
    {
        var draft = new PeriodNoteDraft { Kind = PeriodNoteKind.Response, Text = " xxxxxxxxx " };
        Assert.Equal("period.response-range", draft.ErrorKey);
    }
    [Fact]
    public async Task ResponseSendsTheWholeUserTextOnlyAndLeavesPrefixToBackend()
    {
        var id = Guid.NewGuid(); var text = new string('x', 490);
        await using var provider = Client(async request =>
        {
            Assert.Equal(HttpMethod.Post, request.Method); Assert.Equal($"/api/period-lock/unlock-requests/{id}/respond", request.RequestUri!.AbsolutePath);
            using var body = JsonDocument.Parse(await request.Content!.ReadAsStringAsync());
            Assert.Equal(text, body.RootElement.GetProperty("message").GetString()); Assert.Single(body.RootElement.EnumerateObject());
            return Json(HttpStatusCode.OK, new { message = "fixture" });
        });
        await provider.GetRequiredService<IPeriodApi>().RespondAsync(id, text);
    }
    [Fact]
    public async Task ReadContractsRetainActualCurrentAndPagedRequestsShapes()
    {
        await using var provider = Client(request =>
        {
            Assert.Equal(HttpMethod.Get, request.Method);
            if (request.RequestUri!.AbsolutePath.EndsWith("/current"))
                return Task.FromResult(Json(HttpStatusCode.OK, new { year = 2026, month = 10, monthDisplay = "Oktobar", isLocked = true, lockedBy = "fixture", lockedAt = (DateTime?)null, department = (string?)null }));
            Assert.Equal("/api/period-lock/unlock-requests?status=NEEDS_INFO&page=2&pageSize=100", request.RequestUri.PathAndQuery);
            return Task.FromResult(Json(HttpStatusCode.OK, new { requests = Array.Empty<object>(), total = 101, page = 2, pageSize = 100 }));
        });
        var api = provider.GetRequiredService<IPeriodApi>(); Assert.True((await api.CurrentAsync()).IsLocked);
        Assert.Equal(101, (await api.RequestsAsync("NEEDS_INFO", 2)).Total);
    }
    [Theory]
    [InlineData("lock")]
    [InlineData("unlock")]
    [InlineData("request-unlock")]
    [InlineData("reject")]
    [InlineData("request-info")]
    public async Task ExistingMutationsKeepBackendEndpointsAndPurposeSpecificBodies(string operation)
    {
        var id = Guid.NewGuid();
        await using var provider = Client(async request =>
        {
            var path = operation is "reject" or "request-info" ? $"unlock-requests/{id}/{operation}" : operation;
            Assert.Equal(HttpMethod.Post, request.Method); Assert.Equal("/api/period-lock/" + path, request.RequestUri!.AbsolutePath);
            using var body = JsonDocument.Parse(await request.Content!.ReadAsStringAsync());
            if (operation is "lock" or "unlock") { Assert.Equal(2026, body.RootElement.GetProperty("year").GetInt32()); Assert.Equal(10, body.RootElement.GetProperty("month").GetInt32()); }
            else Assert.Equal("Complete explanation", body.RootElement.GetProperty(operation == "request-unlock" ? "reason" : "note").GetString());
            return Json(HttpStatusCode.OK, new { message = "fixture" });
        });
        var api = provider.GetRequiredService<IPeriodApi>();
        if (operation is "lock" or "unlock") await api.SetLockedAsync(operation == "lock", 2026, 10);
        else if (operation == "request-unlock") await api.RequestUnlockAsync(2026, 10, "Complete explanation");
        else await api.ProcessAsync(id, operation == "reject", "Complete explanation");
    }
    [Theory]
    [InlineData(400)]
    [InlineData(403)]
    public async Task ErrorsKeepStatusWithoutExposingBackendDetails(int status)
    {
        await using var provider = Client(_ => Task.FromResult(Json((HttpStatusCode)status, new { detail = "private-fixture-marker" })));
        var error = await Assert.ThrowsAsync<RegistryApiException>(() => provider.GetRequiredService<IPeriodApi>().RespondAsync(Guid.NewGuid(), "short"));
        Assert.Equal(status, (int?)error.StatusCode); Assert.DoesNotContain("private-fixture-marker", error.ToString());
    }
    private static ServiceProvider Client(Func<HttpRequestMessage, Task<HttpResponseMessage>> respond)
    {
        var services = new ServiceCollection(); services.AddLogging(); services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
        services.AddRbiApiClient<IPeriodApi, PeriodApi>("Period", options => options.BaseUrl = new("https://api.example.test/"))
            .ConfigurePrimaryHttpMessageHandler(() => new Handler(respond));
        return services.BuildServiceProvider();
    }
    private static HttpResponseMessage Json(HttpStatusCode status, object body) => new(status)
    { Content = new StringContent(JsonSerializer.Serialize(body, new JsonSerializerOptions(JsonSerializerDefaults.Web)), Encoding.UTF8, "application/json") };
    private sealed class Handler(Func<HttpRequestMessage, Task<HttpResponseMessage>> respond) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token) => respond(request);
    }
}
