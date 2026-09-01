using System.Net;
using System.Text;

namespace UnitTests.Mocks.HTTP
{
    /// <summary>
    /// Jednostavan HttpMessageHandler za mockiranje HTTP odgovora u Blazor WASM unit testovima.
    /// Ne pravi stvarne mrežne pozive — vraća unaprijed konfigurisan odgovor i pamti zadnji zahtjev.
    /// </summary>
    public class StubHttpMessageHandler : HttpMessageHandler
    {
        private readonly HttpStatusCode _statusCode;
        private readonly string _body;

        public HttpRequestMessage? LastRequest { get; private set; }
        public int CallCount { get; private set; }

        public StubHttpMessageHandler(HttpStatusCode statusCode, string body = "")
        {
            _statusCode = statusCode;
            _body = body;
        }

        public static HttpClient CreateClient(HttpStatusCode statusCode, string body = "")
        {
            return new HttpClient(new StubHttpMessageHandler(statusCode, body))
            {
                BaseAddress = new Uri("https://localhost/")
            };
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequest = request;
            CallCount++;

            var response = new HttpResponseMessage(_statusCode)
            {
                Content = new StringContent(_body, Encoding.UTF8, "application/json")
            };

            return Task.FromResult(response);
        }
    }
}
