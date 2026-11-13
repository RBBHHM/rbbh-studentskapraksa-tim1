using System.Net;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using RBI.ApiCore;
using RBBH.RelatedPartiesRegister.Web.Api;

namespace Web.Blazor.Tests;

public sealed class StructuredBusinessErrorTests
{
    private sealed class Handler(Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token) =>
            Task.FromResult(respond(request));
    }

    private static ServiceProvider Client(string problemJson)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
        services.AddRbiApiClient<ILegalEntitiesApi, LegalEntitiesApi>("Legal",
            options => options.BaseUrl = new("https://api.example.test/"))
            .ConfigurePrimaryHttpMessageHandler(() => new Handler(_ => new(HttpStatusCode.Forbidden)
            {
                Content = new StringContent(problemJson, Encoding.UTF8, "application/problem+json")
            }));
        return services.BuildServiceProvider();
    }

    [Fact]
    public async Task SelfVerificationCodeMapsToSafeLocalizedFourEyesMessage()
    {
        await using var provider = Client("{\"code\":\"verification.self_denied\",\"detail\":\"untrusted private values\"}");
        var failure = await Assert.ThrowsAsync<RegistryApiException>(() =>
            provider.GetRequiredService<ILegalEntitiesApi>().VerifyAsync(Guid.NewGuid()));
        Assert.Equal(HttpStatusCode.Forbidden, failure.StatusCode);
        Assert.Equal("verification.self_denied", failure.Code);
        Assert.Equal("common.verify-own", failure.MessageKey);
        Assert.DoesNotContain("untrusted", failure.ToString());
    }

    [Theory]
    [InlineData("<img src=x>")]
    [InlineData("verification.self_denied\nsecret")]
    [InlineData("Verification.SelfDenied")]
    public async Task InvalidCodeNeverControlsDisplayedMessage(string code)
    {
        await using var provider = Client(System.Text.Json.JsonSerializer.Serialize(new { code, detail = "never show this" }));
        var failure = await Assert.ThrowsAsync<RegistryApiException>(() =>
            provider.GetRequiredService<ILegalEntitiesApi>().VerifyAsync(Guid.NewGuid()));
        Assert.Null(failure.Code);
        Assert.Equal("errors.forbidden", failure.MessageKey);
        Assert.DoesNotContain("never show", failure.ToString());
    }

    [Fact]
    public void HistoricalCreatorCodeMapsOnlyOnMatchingHttpStatus()
    {
        var failure = new RegistryApiException(HttpStatusCode.Conflict, "verification.provenance_required");
        Assert.Equal("errors.verification-provenance-required", failure.MessageKey);
        Assert.Equal("errors.forbidden",
            new RegistryApiException(HttpStatusCode.Forbidden, "verification.provenance_required").MessageKey);
    }

    [Fact]
    public async Task TypedErrorReachesPhysicalVerificationWithoutUpstreamDetails()
    {
        var transport = new Mock<IRbiApiFileTransport>();
        transport.Setup(client => client.SendAsync<RelatedPerson>(HttpMethod.Post,
            It.IsAny<string>(), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(RbiApiResult<RelatedPerson>.Failure(
                new(RbiApiErrorKind.Conflict, HttpStatusCode.Conflict, "Conflict", "Sensitive internal detail")
                { Code = "verification.provenance_required" }));
        var failure = await Assert.ThrowsAsync<RegistryApiException>(() =>
            new RelatedPersonsApi(transport.Object).VerifyAsync(Guid.NewGuid()));
        Assert.Equal("errors.verification-provenance-required", failure.MessageKey);
        Assert.DoesNotContain("Sensitive", failure.Message);
    }
}
