using System.Globalization;
using System.Net;
using RBBH.RelatedPartiesRegister.Web.Api;

namespace Web.Blazor.Tests;

public sealed class ApplicationCultureTests
{
    [Fact]
    public async Task OnePooledHandlerReadsEachCallsCurrentCulture()
    {
        var languages = new List<string>();
        using var client = new HttpClient(new ApplicationCultureHandler { InnerHandler = new Capture(languages) });
        var previous = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("bs");
            await client.GetAsync("https://api.example.test/first");
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("en");
            await client.GetAsync("https://api.example.test/second");
            Assert.Equal(new[] { "bs", "en" }, languages);
        }
        finally { CultureInfo.CurrentUICulture = previous; }
    }
    private sealed class Capture(List<string> languages) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            languages.Add(request.Headers.AcceptLanguage.Single().Value);
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        }
    }
}
