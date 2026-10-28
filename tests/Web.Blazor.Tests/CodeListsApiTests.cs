using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RBI.ApiCore;
using RBBH.RelatedPartiesRegister.Web.Api;
using RBBH.RelatedPartiesRegister.Web.Components.Registry;

namespace Web.Blazor.Tests;

public sealed class CodeListsApiTests
{
    [Theory]
    [InlineData("name", 200, true)]
    [InlineData("name", 201, false)]
    [InlineData("definition", 1, false)]
    [InlineData("definition", 2, true)]
    [InlineData("definition", 100, true)]
    [InlineData("definition", 101, false)]
    [InlineData("description", 500, true)]
    [InlineData("description", 501, false)]
    [InlineData("code", 50, true)]
    [InlineData("code", 51, false)]
    public void EditorUsesDeclaredStorageBoundsWithoutTruncation(string field, int length, bool valid)
    {
        var draft = new CodeListDraft { Definition = field == "definition", Category = "Fixture", Name = "Name", Code = "CODE" };
        var text = new string('X', length);
        if (field is "definition" or "name") draft.Name = text;
        else if (field == "description") draft.Description = text; else draft.Code = text;
        Assert.Equal(valid, draft.Validate().Count == 0);
        Assert.Equal(text, field is "definition" or "name" ? draft.Name : field == "description" ? draft.Description : draft.Code);
    }
    [Theory]
    [InlineData("", true, null)]
    [InlineData("0", true, 0)]
    [InlineData("2147483647", true, 2147483647)]
    [InlineData("2147483648", false, null)]
    [InlineData("-1", false, null)]
    [InlineData("1.5", false, null)]
    [InlineData("12text", false, null)]
    public void OptionalOrderKeepsNullAndZeroDistinctAndRejectsUnrepresentableValues(string order, bool valid, int? expected)
    {
        var draft = new CodeListDraft { Name = "Name", Code = "CODE", Order = order };
        Assert.Equal(expected, draft.ParsedOrder); Assert.Equal(valid, draft.Validate().Count == 0);
    }
    [Fact]
    public void EditKeepsCodeAndCategoryOutOfMutationBodyIncludingInactiveHistory()
    {
        var value = new CodeValue(7, "Category", "IMMUTABLE", "Name", null, 0, false, "fixture", null);
        var draft = CodeListDraft.From(value); Assert.False(draft.Active); Assert.Equal("0", draft.Order);
        using var body = JsonDocument.Parse(JsonSerializer.Serialize(draft.UpdateRequest(), new JsonSerializerOptions(JsonSerializerDefaults.Web)));
        Assert.Equal(4, body.RootElement.EnumerateObject().Count());
        Assert.False(body.RootElement.TryGetProperty("kod", out _)); Assert.False(body.RootElement.TryGetProperty("kategorija", out _));
    }
    [Fact]
    public async Task ActualReadEnvelopesRetainInactiveRowsAndAuditMetadata()
    {
        await using var provider = Client(request => Task.FromResult(request.RequestUri!.AbsolutePath == "/api/code-lists"
            ? Json(200, new { categories = new[] { "Fixture category" } })
            : Json(200, new { category = "Fixture category", items = new[] { new CodeValue(1, "Fixture category", "OLD", "Old value", "History", 0, false, "fixture-actor", new DateTime(2026, 1, 1)) } })));
        var api = provider.GetRequiredService<ICodeListsApi>();
        Assert.Equal("Fixture category", Assert.Single((await api.CategoriesAsync()).Categories));
        var value = Assert.Single((await api.ValuesAsync("Fixture category")).Items); Assert.False(value.IsActive); Assert.Equal("fixture-actor", value.ModifiedBy); Assert.Equal(0, value.DisplayOrder);
    }
    [Theory]
    [InlineData("category-create", "POST", "/api/code-lists/categories")]
    [InlineData("category-delete", "DELETE", "/api/code-lists/categories/Fixture%20category")]
    [InlineData("create", "POST", "/api/code-lists")]
    [InlineData("update", "PUT", "/api/code-lists/7")]
    [InlineData("delete", "DELETE", "/api/code-lists/7")]
    public async Task MutationsUseSpecificSourceEndpointsAndExactBodies(string operation, string method, string path)
    {
        await using var provider = Client(async request =>
        {
            Assert.Equal(method, request.Method.Method); Assert.Equal(path, request.RequestUri!.AbsolutePath);
            if (operation == "create")
            {
                using var body = JsonDocument.Parse(await request.Content!.ReadAsStringAsync());
                Assert.Equal("Fixture category", body.RootElement.GetProperty("kategorija").GetString());
                Assert.Equal("CODE", body.RootElement.GetProperty("kod").GetString()); Assert.True(body.RootElement.GetProperty("aktivan").GetBoolean());
                // Central transport omits null JSON fields. The real optional
                // backend DTO still receives null, distinct from explicit zero.
                Assert.Null(body.RootElement.Deserialize<CreateCodeValue>(new JsonSerializerOptions(JsonSerializerDefaults.Web))!.RedoslijedPrikaza);
            }
            return Json(200, new { message = "fixture" });
        });
        var api = provider.GetRequiredService<ICodeListsApi>();
        switch (operation)
        {
            case "category-create": await api.CreateCategoryAsync("Fixture category", null); break;
            case "category-delete": await api.DeleteCategoryAsync("Fixture category"); break;
            case "create": await api.CreateAsync(new("Fixture category", "CODE", "Name", null, null, true)); break;
            case "update": await api.UpdateAsync(7, new("Updated", null, 0, false)); break;
            case "delete": await api.DeleteAsync(7); break;
        }
    }
    [Fact]
    public async Task PreviewAndConfirmedImportUseFreshOwnedStreamsAndIdenticalBytesWithNoClientExcelParsing()
    {
        var calls = 0; byte[] bytes = [1, 2, 3, 4];
        await using var provider = Client(async request =>
        {
            var dryRun = calls++ == 0;
            Assert.Equal($"/api/code-lists/Fixture%20category/import?dryRun={dryRun.ToString().ToLowerInvariant()}", request.RequestUri!.PathAndQuery);
            var part = Assert.Single(Assert.IsType<MultipartFormDataContent>(request.Content));
            Assert.Equal(bytes, await part.ReadAsByteArrayAsync()); Assert.Equal("file", part.Headers.ContentDisposition!.Name!.Trim('"'));
            return Json(200, new CodeImportPreview(4, ["New", "A B"], ["Existing"], ["Known business validation"], dryRun ? 0 : 1));
        });
        var api = provider.GetRequiredService<ICodeListsApi>();
        var first = new MemoryStream(bytes); var second = new MemoryStream(bytes);
        Assert.Equal(0, (await api.ImportAsync("Fixture category", first, "fixture.xlsx", true)).Imported);
        Assert.Equal(1, (await api.ImportAsync("Fixture category", second, "fixture.xlsx", false)).Imported);
        Assert.False(first.CanRead); Assert.False(second.CanRead); Assert.Equal(2, calls);
    }
    [Theory]
    [InlineData(400)]
    [InlineData(403)]
    [InlineData(503)]
    public async Task FailuresPreserveStatusWithoutLeakingBackendDetails(int status)
    {
        await using var provider = Client(_ => Task.FromResult(Json(status, new { detail = "private-server-marker" })));
        var exception = await Assert.ThrowsAsync<RegistryApiException>(() => provider.GetRequiredService<ICodeListsApi>().ValuesAsync("Category"));
        Assert.Equal(status, (int?)exception.StatusCode); Assert.DoesNotContain("private-server-marker", exception.ToString());
    }
    private static ServiceProvider Client(Func<HttpRequestMessage, Task<HttpResponseMessage>> respond)
    {
        var services = new ServiceCollection(); services.AddLogging(); services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
        services.AddRbiApiClient<ICodeListsApi, CodeListsApi>("Codes", options => options.BaseUrl = new("https://api.example.test/"))
            .ConfigurePrimaryHttpMessageHandler(() => new Handler(respond));
        return services.BuildServiceProvider();
    }
    private static HttpResponseMessage Json(int status, object body) => new((HttpStatusCode)status)
    { Content = new StringContent(JsonSerializer.Serialize(body, new JsonSerializerOptions(JsonSerializerDefaults.Web)), Encoding.UTF8, "application/json") };
    private sealed class Handler(Func<HttpRequestMessage, Task<HttpResponseMessage>> respond) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token) => respond(request);
    }
}
