using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RBI.ApiCore;
using RBBH.RelatedPartiesRegister.Web.Api;
using RBBH.RelatedPartiesRegister.Web.Components.Registry;

namespace Web.Blazor.Tests;

public sealed class UsersApiTests
{
    private static readonly Guid RoleId = Guid.Parse("b756eb6d-ff88-46ee-ab96-c22f12f8c188");
    private static UserDraft Draft() => new()
    {
        Username = " fixture ",
        FirstName = " First ",
        LastName = " Last ",
        Email = " USER@RAIFFEISENGROUP.BA ",
        Roles = [new() { Id = RoleId, Name = "FL", Selected = true }]
    };

    [Theory]
    [InlineData("invalid@example.test")]
    [InlineData("not-an-email")]
    [InlineData("x@y@raiffeisengroup.ba")]
    public void CreateRequiresActualCorporateEmail(string email)
    {
        var draft = Draft(); draft.Email = email;
        Assert.Contains(nameof(UserDraft.Email), draft.Validate().Keys);
        Assert.Throws<InvalidOperationException>(() => draft.ToRequest());
    }
    [Fact]
    public void CreateRequiresNamesAndRolesAndNormalizesOnlySubmittedValues()
    {
        var draft = Draft(); var request = draft.ToRequest();
        Assert.Equal("fixture", request.Username); Assert.Equal("user@raiffeisengroup.ba", request.Email);
        Assert.Equal(new[] { RoleId }, request.RoleIds); Assert.True(request.IsActive);
        draft.FirstName = " "; draft.LastName = new string('x', 101); draft.Roles[0].Selected = false;
        Assert.Contains(nameof(UserDraft.FirstName), draft.Validate().Keys);
        Assert.Contains(nameof(UserDraft.LastName), draft.Validate().Keys);
        Assert.Contains(nameof(UserDraft.Roles), draft.Validate().Keys);
    }
    [Fact]
    public void RoleEditAllowsEmptySelectionAndRetainsActualCatalogIdsNotRealmRoles()
    {
        var user = new ApplicationUser(Guid.NewGuid(), "u", "First", "Last", "u@example.test", ["FL"], [], true);
        var draft = UserDraft.From(user, new([new(RoleId, "FL", 1)]));
        Assert.Equal(new[] { RoleId }, draft.SelectedRoles);
        draft.Roles[0].Selected = false; Assert.Empty(draft.Validate()); Assert.Empty(draft.SelectedRoles);
        Assert.Throws<InvalidOperationException>(() => draft.ToRequest());
    }
    [Fact]
    public async Task ListAndRoleCatalogUseTheRealNamedCollectionsAndSourcePageSize()
    {
        await using var provider = Client(async request =>
        {
            Assert.Equal(HttpMethod.Get, request.Method);
            if (request.RequestUri!.AbsolutePath == "/api/roles")
                return Json(HttpStatusCode.OK, new { roles = new[] { new { id = RoleId, name = "FL", userCount = 3 } } });
            Assert.Equal("/api/users?page=2&pageSize=20", request.RequestUri.PathAndQuery);
            await Task.CompletedTask;
            return Json(HttpStatusCode.OK, new { users = Array.Empty<object>(), total = 21, page = 2, pageSize = 20 });
        });
        var api = provider.GetRequiredService<IUsersApi>();
        Assert.Equal(21, (await api.ListAsync(2)).Total);
        Assert.Equal(RoleId, Assert.Single((await api.RolesAsync()).Roles).Id);
    }
    [Fact]
    public async Task CreationUsesLocalApplicationRoleIdsAndDoesNotInventPasswordOrRealmRoles()
    {
        var request = Draft().ToRequest();
        await using var provider = Client(async message =>
        {
            Assert.Equal(HttpMethod.Post, message.Method); Assert.Equal("/api/users", message.RequestUri!.AbsolutePath);
            using var body = JsonDocument.Parse(await message.Content!.ReadAsStringAsync());
            Assert.Equal(RoleId, body.RootElement.GetProperty("roleIds")[0].GetGuid());
            Assert.False(body.RootElement.TryGetProperty("password", out _)); Assert.False(body.RootElement.TryGetProperty("realmRoles", out _));
            return Json(HttpStatusCode.Created, new ApplicationUser(Guid.NewGuid(), "fixture", "First", "Last", request.Email, ["FL"], ["FL_VIEW"], true));
        });
        Assert.Equal("FL", Assert.Single((await provider.GetRequiredService<IUsersApi>().CreateAsync(request)).Roles));
    }
    [Theory]
    [InlineData("roles", "PUT", "/role")]
    [InlineData("reactivate", "POST", "/reactivate")]
    [InlineData("deactivate", "DELETE", "")]
    [InlineData("delete", "DELETE", "/permanent")]
    public async Task MutationsRetainExistingMethodAndIdentifierContract(string operation, string method, string suffix)
    {
        var id = Guid.NewGuid();
        await using var provider = Client(async request =>
        {
            Assert.Equal(method, request.Method.Method); Assert.Equal($"/api/users/{id}{suffix}", request.RequestUri!.AbsolutePath);
            if (operation == "roles")
            {
                using var body = JsonDocument.Parse(await request.Content!.ReadAsStringAsync());
                Assert.Equal(0, body.RootElement.GetProperty("roleIds").GetArrayLength());
            }
            return Json(HttpStatusCode.OK, new { message = "fixture" });
        });
        var api = provider.GetRequiredService<IUsersApi>();
        if (operation == "roles") await api.UpdateRolesAsync(id, []);
        else if (operation == "delete") await api.DeleteAsync(id);
        else await api.SetActiveAsync(id, operation == "reactivate");
    }
    [Theory]
    [InlineData(400)]
    [InlineData(403)]
    [InlineData(409)]
    public async Task RejectedMutationKeepsStatusWithoutExposingRawBackendDetails(int status)
    {
        await using var provider = Client(_ => Task.FromResult(Json((HttpStatusCode)status, new { detail = "private-fixture-marker" })));
        var error = await Assert.ThrowsAsync<RegistryApiException>(() => provider.GetRequiredService<IUsersApi>().UpdateRolesAsync(Guid.NewGuid(), []));
        Assert.Equal(status, (int?)error.StatusCode); Assert.DoesNotContain("private-fixture-marker", error.ToString());
    }
    private static ServiceProvider Client(Func<HttpRequestMessage, Task<HttpResponseMessage>> respond)
    {
        var services = new ServiceCollection(); services.AddLogging();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
        services.AddRbiApiClient<IUsersApi, UsersApi>("Users", options => options.BaseUrl = new("https://api.example.test/"))
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
