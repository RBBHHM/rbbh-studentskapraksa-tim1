using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RBI.ApiCore;
using RBBH.RelatedPartiesRegister.Web.Api;

namespace Web.Blazor.Tests;

public sealed class LimitsApiTests
{
    private static ServiceProvider Client(Func<HttpRequestMessage, Task<HttpResponseMessage>> send)
    {
        var services = new ServiceCollection(); services.AddLogging();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
        services.AddRbiApiClient<ILimitsApi, LimitsApi>("Limits", options => options.BaseUrl = new("https://api.example.test/"))
            .ConfigurePrimaryHttpMessageHandler(() => new Handler(send));
        return services.BuildServiceProvider();
    }
    [Fact]
    public async Task FilteredReadAndExportUseSameBackendEntityFilter()
    {
        var id = Guid.NewGuid(); var calls = 0;
        await using var provider = Client(request =>
        {
            Assert.Equal($"?legalEntityId={id:D}", request.RequestUri!.Query); calls++;
            if (request.RequestUri.AbsolutePath.EndsWith("/export"))
            {
                var response = new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent([0x50, 0x4b]) };
                response.Content.Headers.ContentType = new("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
                response.Content.Headers.ContentDisposition = new("attachment") { FileName = "limits.xlsx" }; return Task.FromResult(response);
            }
            Assert.Equal("/api/limiti", request.RequestUri.AbsolutePath); return Task.FromResult(Json("[]"));
        });
        var api = provider.GetRequiredService<ILimitsApi>(); Assert.Empty(await api.ListAsync(id));
        Assert.Equal("limits.xlsx", (await api.ExportAsync(id)).FileName); Assert.Equal(2, calls);
    }
    [Fact]
    public async Task LookupEscapesQueryAndFormDataComesFromBackend()
    {
        var id = Guid.NewGuid();
        await using var provider = Client(request =>
        {
            if (request.RequestUri!.AbsolutePath.EndsWith("/search"))
            {
                Assert.Equal("?search=A%2FB%20%26%20C", request.RequestUri.Query); return Task.FromResult(Json("[]"));
            }
            Assert.Equal($"/api/legal-entities/{id:D}/limit-form-data", request.RequestUri.AbsolutePath);
            return Task.FromResult(Json($"{{\"id\":\"{id}\",\"name\":\"Company\",\"regulatoryCapital\":123.45,\"coreCapital\":67.89}}"));
        });
        var api = provider.GetRequiredService<ILimitsApi>(); Assert.Empty(await api.SearchEntitiesAsync("A/B & C"));
        Assert.Equal(123.45m, (await api.EntityDataAsync(id)).RegulatoryCapital);
    }
    [Fact]
    public async Task IntegerIdUpdateSendsOnlyEditableFieldsNotDerivedCapital()
    {
        await using var provider = Client(async request =>
        {
            Assert.Equal(HttpMethod.Put, request.Method); Assert.Equal("/api/limiti/12", request.RequestUri!.AbsolutePath);
            using var body = JsonDocument.Parse(await request.Content!.ReadAsStringAsync());
            Assert.Equal(0m, body.RootElement.GetProperty("odobreniLimit").GetDecimal());
            Assert.False(body.RootElement.TryGetProperty("regulatorniKapital", out _));
            return Json("{\"id\":12,\"naziv\":\"Company\",\"tipLimita\":\"REG\"}");
        });
        Assert.Equal(12, (await provider.GetRequiredService<ILimitsApi>().SaveAsync(12, new(Guid.NewGuid(), "REG", 0, null, null, "Comment"))).Id);
    }
    [Theory]
    [InlineData(400, "errors.validation")]
    [InlineData(403, "errors.forbidden")]
    [InlineData(423, "errors.locked")]
    public async Task SafeFailuresPreserveStatusWithoutRenderingInternalDetails(int status, string key)
    {
        await using var provider = Client(_ => Task.FromResult(new HttpResponseMessage((HttpStatusCode)status) { Content = new StringContent("{\"detail\":\"SQL private-marker\"}", Encoding.UTF8, "application/problem+json") }));
        var error = await Assert.ThrowsAsync<RegistryApiException>(() => provider.GetRequiredService<ILimitsApi>().DeleteAsync(1));
        Assert.Equal(key, error.MessageKey); Assert.DoesNotContain("private-marker", error.ToString());
    }
    private static HttpResponseMessage Json(string body) => new(HttpStatusCode.OK) { Content = new StringContent(body, Encoding.UTF8, "application/json") };
    private sealed class Handler(Func<HttpRequestMessage, Task<HttpResponseMessage>> send) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token) => send(request);
    }
}
