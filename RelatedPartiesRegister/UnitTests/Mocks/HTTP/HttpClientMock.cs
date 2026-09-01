using NSubstitute;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace UnitTests.Mocks.HTTP
{
    public class HttpClientMock
    {
        // Helper to create a HttpClient with a fake response
        public static HttpClient CreateHttpClientWithResponse(object? contentObject, HttpStatusCode statusCode)
        {
            var handler = Substitute.ForPartsOf<FakeHttpMessageHandler>();
            if (contentObject != null)
            {
                handler.ConfigureResponse(statusCode, contentObject);
            }
            else
            {
                handler.ConfigureResponse(statusCode, null);
            }
            return new HttpClient(handler)
            {
                BaseAddress = new System.Uri("https://fakewebsite.com/")
            };
        }
    }
}
