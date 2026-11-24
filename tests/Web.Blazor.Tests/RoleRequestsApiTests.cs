using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RBI.ApiCore;
using RBBH.RelatedPartiesRegister.Web.Api;

namespace Web.Blazor.Tests;

public sealed class RoleRequestsApiTests
{
    private static ServiceProvider Client(Func<HttpRequestMessage, string?, HttpResponseMessage> respond)
    {
        var services = new ServiceCollection(); services.AddLogging();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
        services.AddRbiApiClient<IRoleRequestsApi, RoleRequestsApi>("RoleRequests", options => options.BaseUrl = new("https://api.example.test/"))
            .ConfigurePrimaryHttpMessageHandler(() => new Handler(respond));
        return services.BuildServiceProvider();
    }
    private static HttpResponseMessage Json(HttpStatusCode status, string body) => new(status) { Content = new StringContent(body, Encoding.UTF8, "application/json") };
    private const string RequestJson = "{\"id\":\"22222222-2222-2222-2222-222222222222\",\"purpose\":\"new\",\"status\":\"pending\",\"roleIds\":[\"33333333-3333-3333-3333-333333333333\"],\"roleNames\":[\"FL\"],\"note\":null,\"createdAt\":\"2026-11-03T10:00:00Z\",\"processedAt\":null,\"processedBy\":null,\"decisionNote\":null,\"version\":3,\"requesterUsername\":\"requester\",\"requesterEmail\":\"requester@example.test\"}";

    [Fact]
    public async Task SubmitSendsPurposeRoleIdsAndTheClientRequestIdForReplaySafety()
    {
        string? body = null; string? path = null;
        await using var provider = Client((request, text) => { body = text; path = request.RequestUri!.AbsolutePath; return Json(HttpStatusCode.Created, RequestJson); });
        var clientId = Guid.NewGuid(); var role = Guid.NewGuid();
        var created = await provider.GetRequiredService<IRoleRequestsApi>().SubmitAsync(clientId, "change", [role], "napomena");
        Assert.Equal("/api/role-requests", path); Assert.Equal("pending", created.Request!.Status);
        using var document = JsonDocument.Parse(body!);
        Assert.Equal(clientId, document.RootElement.GetProperty("clientRequestId").GetGuid()); Assert.Equal("change", document.RootElement.GetProperty("purpose").GetString());
        Assert.Equal(role, document.RootElement.GetProperty("roleIds")[0].GetGuid()); Assert.Equal("napomena", document.RootElement.GetProperty("note").GetString());
    }
    [Theory]
    [InlineData(true, "approve")]
    [InlineData(false, "reject")]
    public async Task DecisionsCarryTheExpectedVersionAndAStableDecisionIdSoAReplayIsNotASecondGrant(bool approve, string action)
    {
        var bodies = new List<string>(); string? path = null;
        await using var provider = Client((request, text) => { bodies.Add(text!); path = request.RequestUri!.AbsolutePath; return Json(HttpStatusCode.OK, RequestJson); });
        var api = provider.GetRequiredService<IRoleRequestsApi>();
        var request = JsonSerializer.Deserialize<RoleRequest>(RequestJson, new JsonSerializerOptions(JsonSerializerDefaults.Web))!;
        await api.DecideAsync(request, approve, "ok"); await api.DecideAsync(request, approve, "ok");
        Assert.Equal($"/api/role-requests/{request.Id}/{action}", path);
        using var first = JsonDocument.Parse(bodies[0]); using var second = JsonDocument.Parse(bodies[1]);
        Assert.Equal(3, first.RootElement.GetProperty("expectedVersion").GetInt32());
        Assert.Equal(first.RootElement.GetProperty("decisionId").GetGuid(), second.RootElement.GetProperty("decisionId").GetGuid());
        Assert.NotEqual(Guid.Empty, first.RootElement.GetProperty("decisionId").GetGuid());
    }
    [Fact]
    public async Task ListsUseTheOwnAndReviewRoutesWithStatusFilter()
    {
        var paths = new List<string>();
        await using var provider = Client((request, _) => { paths.Add(request.RequestUri!.PathAndQuery); return Json(HttpStatusCode.OK, "{\"items\":[],\"total\":0,\"page\":1,\"pageSize\":20}"); });
        var api = provider.GetRequiredService<IRoleRequestsApi>(); await api.MineAsync(1, ""); await api.ReviewAsync(2, "pending");
        Assert.Equal(["/api/role-requests/mine?page=1&pageSize=20&status=", "/api/role-requests?page=2&pageSize=20&status=pending"], paths);
    }
    [Theory]
    [InlineData(403, null, "errors.forbidden")]
    [InlineData(409, null, "errors.conflict")]
    [InlineData(409, "role_request.pending_exists", "roles.pending-exists")]
    [InlineData(403, "role_request.self_decision_denied", "roles.error-self")]
    public async Task ConflictsAndDenialsKeepOwnedCopy(int status, string? code, string key)
    {
        await using var provider = Client((_, _) => Json((HttpStatusCode)status, JsonSerializer.Serialize(new { code, detail = "private-marker" })));
        var api = provider.GetRequiredService<IRoleRequestsApi>();
        var request = JsonSerializer.Deserialize<RoleRequest>(RequestJson, new JsonSerializerOptions(JsonSerializerDefaults.Web))!;
        var submit = await api.SubmitAsync(Guid.NewGuid(), "new", [Guid.NewGuid()], null);
        var decision = await api.DecideAsync(request, true, null);
        Assert.Null(submit.Request);
        Assert.Null(decision.Request);
        Assert.Equal(key, submit.ErrorKey);
        Assert.Equal(key, decision.ErrorKey);
    }
    private sealed class Handler(Func<HttpRequestMessage, string?, HttpResponseMessage> respond) : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token) =>
            respond(request, request.Content is null ? null : await request.Content.ReadAsStringAsync(token));
    }
}
