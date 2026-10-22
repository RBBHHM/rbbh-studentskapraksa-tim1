using System.Net;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RBI.ApiCore;
using RBBH.RelatedPartiesRegister.Web.Api;

namespace Web.Blazor.Tests;

public sealed class ReportsApiTests
{
    private static ServiceProvider Client(Func<HttpRequestMessage, HttpResponseMessage> send)
    {
        var services = new ServiceCollection(); services.AddLogging();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
        services.AddRbiApiClient<IReportsApi, ReportsApi>("Reports", options => options.BaseUrl = new("https://api.example.test/"))
            .ConfigurePrimaryHttpMessageHandler(() => new Handler(send));
        return services.BuildServiceProvider();
    }
    [Theory]
    [InlineData(ReportKind.Daily, "daily")]
    [InlineData(ReportKind.Monthly, "monthly")]
    public async Task ArchiveUsesTypedRawPagingAndSourcePageSize(ReportKind kind, string path)
    {
        await using var provider = Client(request =>
        {
            Assert.Equal($"/api/reports/{path}?page=2&pageSize=100", request.RequestUri!.PathAndQuery);
            return Json("{\"items\":[],\"total\":101,\"page\":2,\"pageSize\":100}");
        });
        var archive = await provider.GetRequiredService<IReportsApi>().ListAsync(kind, 2);
        Assert.Equal(101, archive.Total); Assert.Equal(2, archive.Page); Assert.Empty(archive.Items);
    }
    [Fact]
    public async Task PersistedGenerationIsPostAndNeverAutomaticallyRetried()
    {
        var calls = 0;
        await using var provider = Client(request =>
        {
            calls++; Assert.Equal(HttpMethod.Post, request.Method); Assert.Equal("/api/reports/daily", request.RequestUri!.AbsolutePath);
            return new(HttpStatusCode.ServiceUnavailable) { Content = new StringContent("{\"detail\":\"private-marker\"}", Encoding.UTF8, "application/problem+json") };
        });
        var error = await Assert.ThrowsAsync<RegistryApiException>(() => provider.GetRequiredService<IReportsApi>().GenerateDailyAsync());
        Assert.Equal(1, calls); Assert.DoesNotContain("private-marker", error.ToString());
    }
    [Fact]
    public async Task MonthlyCreationPreservesServerIdentityAndSummaryWithoutClientCalculations()
    {
        var id = Guid.NewGuid();
        await using var provider = Client(request =>
        {
            Assert.Equal(HttpMethod.Post, request.Method); Assert.Equal("/api/reports/monthly/2000/12", request.RequestUri!.AbsolutePath);
            return Json($"{{\"id\":\"{id}\",\"reportType\":\"MONTHLY\",\"createdBy\":\"backend-author\",\"totalClients\":3,\"totalExposure\":75.5}}");
        });
        var report = await provider.GetRequiredService<IReportsApi>().GenerateMonthlyAsync(2000, 12);
        Assert.Equal(id, report.Id); Assert.Equal("backend-author", report.CreatedBy); Assert.Equal(75.5m, report.TotalExposure);
    }
    [Theory]
    [InlineData(1999, 1)]
    [InlineData(2000, 0)]
    [InlineData(2000, 13)]
    public async Task InvalidPeriodCannotSendAPost(int year, int month)
    {
        await using var provider = Client(_ => throw new InvalidOperationException("No request is permitted."));
        await Assert.ThrowsAnyAsync<ArgumentOutOfRangeException>(() => provider.GetRequiredService<IReportsApi>().GenerateMonthlyAsync(year, month));
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => provider.GetRequiredService<IReportsApi>().GenerateMonthlyAsync(DateTime.UtcNow.Year + 1, 1));
    }
    [Theory]
    [InlineData("1", true)]
    [InlineData(" 1234567890123 ", true)]
    [InlineData("12345678901234", false)]
    [InlineData("1/2", false)]
    [InlineData("1e3", false)]
    [InlineData("", false)]
    public void ClientIdentifierKeepsTheExistingNumericContract(string value, bool valid) => Assert.Equal(valid, ReportsApi.ValidIdentifier(value));
    [Fact]
    public async Task SnapshotDownloadUsesBackendAttachmentNotRegeneration()
    {
        var id = Guid.NewGuid();
        await using var provider = Client(request =>
        {
            Assert.Equal(HttpMethod.Get, request.Method); Assert.Equal($"/api/reports/export/generated/{id:D}", request.RequestUri!.AbsolutePath);
            var response = new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent([0x50, 0x4b]) };
            response.Content.Headers.ContentType = new("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
            response.Content.Headers.ContentDisposition = new("attachment") { FileName = "stored-report.xlsx" }; return response;
        });
        Assert.Equal("stored-report.xlsx", (await provider.GetRequiredService<IReportsApi>().DownloadAsync(id)).FileName);
    }
    private static HttpResponseMessage Json(string body) => new(HttpStatusCode.OK) { Content = new StringContent(body, Encoding.UTF8, "application/json") };
    private sealed class Handler(Func<HttpRequestMessage, HttpResponseMessage> send) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token) => Task.FromResult(send(request));
    }
}
