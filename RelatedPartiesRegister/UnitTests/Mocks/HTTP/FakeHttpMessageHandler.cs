using System.Net;

namespace UnitTests.Mocks.HTTP
{
    // Helper HttpMessageHandler for faking responses
    public class FakeHttpMessageHandler : HttpMessageHandler
    {
        private HttpResponseMessage _response = new(HttpStatusCode.OK);

        public void ConfigureResponse(HttpStatusCode statusCode, object? content)
        {
            _response = new HttpResponseMessage(statusCode);
            if (content != null)
            {
                string json = System.Text.Json.JsonSerializer.Serialize(content);
                _response.Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
            }
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(_response);
        }
    }
}
