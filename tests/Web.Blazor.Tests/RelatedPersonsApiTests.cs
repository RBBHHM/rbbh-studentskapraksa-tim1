using System.Net;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RBI.ApiCore;
using RBBH.RelatedPartiesRegister.Web.Api;

namespace Web.Blazor.Tests;

// Consumes real packed RBI assemblies (no platform ProjectReference).
public sealed class RelatedPersonsApiTests
{
    private static ServiceProvider Client(Func<HttpRequestMessage, Task<HttpResponseMessage>> send)
    {
        var services = new ServiceCollection(); services.AddLogging();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
        services.AddRbiApiClient<IRelatedPersonsApi, RelatedPersonsApi>("Persons", options => options.BaseUrl = new("https://api.example.test/"))
            .ConfigurePrimaryHttpMessageHandler(() => new StubHandler(send));
        return services.BuildServiceProvider();
    }

    [Fact]
    public async Task ListUsesRawDetailedContractNotAnEnvelopeNormalizer()
    {
        await using var services = Client(request =>
        {
            Assert.Equal("/api/related-persons/detailed", request.RequestUri!.AbsolutePath);
            return Task.FromResult(Json("[]"));
        });
        Assert.Empty(await services.GetRequiredService<IRelatedPersonsApi>().ListAsync());
    }

    [Fact]
    public async Task DuplicateQueryEscapesValueAndPreservesEditExclusion()
    {
        var id = Guid.NewGuid();
        await using var services = Client(request =>
        {
            Assert.Equal($"?passportNumber=AB%2F123&excludeId={id:D}", request.RequestUri!.Query);
            return Task.FromResult(Json("{\"exists\":true,\"field\":\"passportNumber\",\"message\":\"backend-owned\"}"));
        });
        Assert.True((await services.GetRequiredService<IRelatedPersonsApi>().CheckDuplicateAsync("passportNumber", "AB/123", id)).Exists);
    }

    [Fact]
    public async Task PackedFileTransportActuallyPostsMultipartAndReturnsImportCounts()
    {
        await using var services = Client(async request =>
        {
            Assert.Equal("/api/related-persons/import", request.RequestUri!.AbsolutePath);
            Assert.Equal("multipart/form-data", request.Content!.Headers.ContentType!.MediaType);
            Assert.Contains("test.xlsx", await request.Content.ReadAsStringAsync());
            return Json("{\"imported\":2,\"failed\":1,\"errors\":[\"Red 3: invalid\"]}");
        });
        var result = await services.GetRequiredService<IRelatedPersonsApi>().ImportAsync(new MemoryStream([0x50, 0x4b]), "test.xlsx");
        Assert.Equal(2, result.Imported); Assert.Equal(1, result.Failed);
    }

    [Fact]
    public async Task PackedFileTransportPreservesBackendAttachment()
    {
        await using var services = Client(request =>
        {
            Assert.Equal("/api/related-persons/export", request.RequestUri!.AbsolutePath);
            var response = new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent([0x50, 0x4b]) };
            response.Content.Headers.ContentDisposition = new("attachment") { FileName = "physical.xlsx" };
            response.Content.Headers.ContentType = new("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
            return Task.FromResult(response);
        });
        var result = await services.GetRequiredService<IRelatedPersonsApi>().ExportAsync();
        Assert.Equal("physical.xlsx", result.FileName); Assert.Equal(new byte[] { 0x50, 0x4b }, result.Content);
    }

    [Theory]
    [InlineData(403, "errors.forbidden")]
    [InlineData(423, "errors.locked")]
    [InlineData(409, "errors.conflict")]
    public async Task SafeErrorsKeepStatusButNeverRenderInternalDetail(int status, string messageKey)
    {
        await using var services = Client(_ => Task.FromResult(new HttpResponseMessage((HttpStatusCode)status)
        {
            Content = new StringContent("{\"detail\":\"SQL password-secret stack trace\"}", Encoding.UTF8, "application/problem+json")
        }));
        var error = await Assert.ThrowsAsync<RegistryApiException>(() => services.GetRequiredService<IRelatedPersonsApi>().DeleteAsync(Guid.NewGuid()));
        Assert.Equal(messageKey, error.MessageKey); Assert.DoesNotContain("password-secret", error.ToString());
    }

    private static HttpResponseMessage Json(string json) => new(HttpStatusCode.OK) { Content = new StringContent(json, Encoding.UTF8, "application/json") };
    private sealed class StubHandler(Func<HttpRequestMessage, Task<HttpResponseMessage>> send) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) => send(request);
    }
}
