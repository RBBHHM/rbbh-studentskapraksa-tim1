using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RBI.ApiCore;
using RBBH.RelatedPartiesRegister.Web.Api;

namespace Web.Blazor.Tests;

public sealed class LegalEntitiesApiTests
{
    private static ServiceProvider Client(Func<HttpRequestMessage, Task<HttpResponseMessage>> send)
    {
        var services = new ServiceCollection(); services.AddLogging();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
        services.AddRbiApiClient<ILegalEntitiesApi, LegalEntitiesApi>("Legal", options => options.BaseUrl = new("https://api.example.test/"))
            .ConfigurePrimaryHttpMessageHandler(() => new Handler(send));
        return services.BuildServiceProvider();
    }
    [Fact]
    public async Task RawPagedContractIsNotNormalizedAndUsesExplicitPage()
    {
        await using var provider = Client(request =>
        {
            Assert.Equal("/api/legal-entities?page=2&pageSize=20", request.RequestUri!.PathAndQuery);
            return Task.FromResult(Json("{\"items\":[],\"total\":21,\"page\":2,\"pageSize\":20}"));
        });
        var result = await provider.GetRequiredService<ILegalEntitiesApi>().ListAsync(2);
        Assert.Equal(21, result.Total); Assert.Equal(2, result.Page); Assert.Empty(result.Items);
    }
    [Fact]
    public async Task UpdateRetainsHiddenMatbrojAndDoesNotOverrideVerificationStatus()
    {
        var id = Guid.NewGuid();
        await using var provider = Client(async request =>
        {
            Assert.Equal(HttpMethod.Put, request.Method); Assert.Equal($"/api/legal-entities/{id:D}", request.RequestUri!.AbsolutePath);
            using var body = JsonDocument.Parse(await request.Content!.ReadAsStringAsync());
            Assert.Equal("ORIGINAL", body.RootElement.GetProperty("matbroj").GetString());
            Assert.False(body.RootElement.TryGetProperty("status", out _));
            return Json("{\"id\":\"" + id + "\",\"name\":\"Updated\",\"isResident\":false,\"status\":\"Verified\"}");
        });
        await provider.GetRequiredService<ILegalEntitiesApi>().SaveAsync(id, new(false, null, "12345", "123456", "Updated", "ORIGINAL", "ZOB-2-V-5", "Description", true, new(2026, 1, 1), null));
    }
    [Fact]
    public async Task ImportUsesApprovedMultipartTransport()
    {
        await using var provider = Client(async request =>
        {
            Assert.Equal("/api/legal-entities/import", request.RequestUri!.AbsolutePath);
            Assert.Equal("multipart/form-data", request.Content!.Headers.ContentType!.MediaType);
            Assert.Contains("legal.xlsx", await request.Content.ReadAsStringAsync());
            return Json("{\"imported\":1,\"failed\":0,\"errors\":[]}");
        });
        Assert.Equal(1, (await provider.GetRequiredService<ILegalEntitiesApi>().ImportAsync(new MemoryStream([0x50, 0x4b]), "legal.xlsx")).Imported);
    }
    [Theory]
    [InlineData(400, "errors.validation")]
    [InlineData(403, "errors.forbidden")]
    [InlineData(423, "errors.locked")]
    public async Task FailureNeverExposesBackendExceptionDetail(int status, string key)
    {
        await using var provider = Client(_ => Task.FromResult(new HttpResponseMessage((HttpStatusCode)status) { Content = new StringContent("{\"detail\":\"SQL private-marker\"}", Encoding.UTF8, "application/problem+json") }));
        var error = await Assert.ThrowsAsync<RegistryApiException>(() => provider.GetRequiredService<ILegalEntitiesApi>().DeleteAsync(Guid.NewGuid()));
        Assert.Equal(key, error.MessageKey); Assert.DoesNotContain("private-marker", error.ToString());
    }
    private static HttpResponseMessage Json(string body) => new(HttpStatusCode.OK) { Content = new StringContent(body, Encoding.UTF8, "application/json") };
    private sealed class Handler(Func<HttpRequestMessage, Task<HttpResponseMessage>> send) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token) => send(request);
    }
}
