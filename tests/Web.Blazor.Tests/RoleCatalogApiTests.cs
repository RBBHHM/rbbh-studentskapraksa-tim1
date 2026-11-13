using System.Net;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RBI.ApiCore;
using RBBH.RelatedPartiesRegister.Web.Api;

namespace Web.Blazor.Tests;

public sealed class RoleCatalogApiTests
{
    private sealed class Handler(Func<HttpRequestMessage, HttpResponseMessage> send) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token) =>
            Task.FromResult(send(request));
    }

    private static ServiceProvider Client(Func<HttpRequestMessage, HttpResponseMessage> send)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
        services.AddRbiApiClient<IRoleCatalogApi, RoleCatalogApi>("RoleCatalog",
            options => options.BaseUrl = new("https://api.example.test/"))
            .ConfigurePrimaryHttpMessageHandler(() => new Handler(send));
        return services.BuildServiceProvider();
    }

    private static HttpResponseMessage Json(HttpStatusCode status, string data) =>
        new(status) { Content = new StringContent(data, Encoding.UTF8, "application/json") };

    [Fact]
    public async Task ListsAndFiltersRolesUsingBackendCatalog()
    {
        string? url = null;
        await using var provider = Client(request =>
        {
            url = request.RequestUri!.PathAndQuery;
            return Json(HttpStatusCode.OK, "{\"items\":[{\"id\":\"11111111-1111-1111-1111-111111111111\",\"name\":\"custom\",\"displayName\":\"Custom\",\"description\":null,\"isActive\":true,\"version\":1,\"permissionIds\":[],\"isProtected\":false}],\"total\":1,\"page\":2,\"pageSize\":20}");
        });
        var page = await provider.GetRequiredService<IRoleCatalogApi>().ListAsync(2, "A&B");
        Assert.Equal("/api/role-catalog?page=2&pageSize=20&search=A%26B", url);
        Assert.Equal("custom", Assert.Single(page.Items).Name);
    }

    [Fact]
    public async Task UsesBackendPermissionCatalogInsteadOfHardcodedRoleOptions()
    {
        await using var provider = Client(request =>
        {
            Assert.Equal("/api/role-catalog/permissions", request.RequestUri!.AbsolutePath);
            return Json(HttpStatusCode.OK, "{\"items\":[{\"id\":\"11111111-1111-1111-1111-111111111111\",\"code\":\"FL_VIEW\",\"module\":\"FL\",\"description\":null}]}");
        });
        var response = await provider.GetRequiredService<IRoleCatalogApi>().PermissionsAsync();
        Assert.Equal("FL_VIEW", Assert.Single(response.Items).Code);
    }

    [Fact]
    public async Task CreateUsesRoleCatalogEndpointAndPermissionIdentifiers()
    {
        HttpMethod? method = null; string? path = null; string? payload = null;
        await using var provider = Client(request =>
        {
            method = request.Method; path = request.RequestUri!.AbsolutePath;
            payload = request.Content!.ReadAsStringAsync().GetAwaiter().GetResult();
            return Json(HttpStatusCode.Created, "{\"id\":\"11111111-1111-1111-1111-111111111111\",\"name\":\"custom\",\"displayName\":\"Custom\",\"isActive\":true,\"version\":1,\"permissionIds\":[],\"isProtected\":false}");
        });
        await provider.GetRequiredService<IRoleCatalogApi>().CreateAsync(new("custom", "Custom", null, []));
        Assert.Equal(HttpMethod.Post, method); Assert.Equal("/api/role-catalog", path);
        Assert.Contains("\"permissionIds\"", payload);
    }

    [Fact]
    public async Task UpdateSuppliesOptimisticConcurrencyVersion()
    {
        var id = Guid.Parse("11111111-1111-1111-1111-111111111111");
        string? path = null; string? payload = null; HttpMethod? method = null;
        await using var provider = Client(request =>
        {
            method = request.Method; path = request.RequestUri!.AbsolutePath;
            payload = request.Content!.ReadAsStringAsync().GetAwaiter().GetResult();
            return Json(HttpStatusCode.OK, "{\"id\":\"11111111-1111-1111-1111-111111111111\",\"name\":\"custom\",\"displayName\":\"Changed\",\"isActive\":true,\"version\":4,\"permissionIds\":[],\"isProtected\":false}");
        });
        await provider.GetRequiredService<IRoleCatalogApi>().UpdateAsync(id, new("Changed", null, true, 3, []));
        Assert.Equal(HttpMethod.Put, method);
        Assert.Equal($"/api/role-catalog/{id:D}", path);
        Assert.Contains("\"expectedVersion\":3", payload);
    }

    [Theory]
    [InlineData(HttpStatusCode.Forbidden)]
    [InlineData(HttpStatusCode.Conflict)]
    public async Task DeniedOrStaleUpdateNeverAppearsSuccessful(HttpStatusCode status)
    {
        await using var provider = Client(_ => Json(status, "{\"detail\":\"private-identity\"}"));
        var error = await Assert.ThrowsAsync<RegistryApiException>(() =>
            provider.GetRequiredService<IRoleCatalogApi>().UpdateAsync(Guid.NewGuid(), new("Changed", null, true, 1, [])));
        Assert.Equal(status, error.StatusCode);
        Assert.DoesNotContain("private-identity", error.ToString());
    }
}
