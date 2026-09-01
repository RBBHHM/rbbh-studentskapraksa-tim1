using RBBH.ConnectedParties.Helpers.Constants;
using RBBH.ConnectedParties.IoC.Extensions.HTTP;
using Microsoft.AspNetCore.Http;
using NSubstitute;

namespace UnitTests.Extensions
{

    public class HeaderPropagationHandlerTests
    {
        private class TestableHeaderPropagationHandler : HeaderPropagationHandler
        {
            public TestableHeaderPropagationHandler(IHttpContextAccessor httpContextAccessor)
                : base(httpContextAccessor)
            {
                // Set the inner handler to a dummy handler
                InnerHandler = new HttpClientHandler();
            }

            public Task<HttpResponseMessage> PublicSendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                return SendAsync(request, cancellationToken);
            }
        }

        [Fact]
        public async Task PublicSendAsync_AddsCorrelationID_WhenHeaderExists()
        {
            // Arrange
            var correlationId = "test-correlation-id";
            var httpContextAccessor = Substitute.For<IHttpContextAccessor>();
            var httpContext = new DefaultHttpContext();
            httpContext.Request.Headers[AppConstants.CORRELATION_ID] = correlationId;

            httpContextAccessor.HttpContext.Returns(httpContext);

            var handler = new TestableHeaderPropagationHandler(httpContextAccessor);
            var request = new HttpRequestMessage(HttpMethod.Get, "https://example.com");

            // Act
            var response = await handler.PublicSendAsync(request, CancellationToken.None);

            // Assert
            Assert.True(request.Headers.Contains(AppConstants.CORRELATION_ID));
            Assert.Contains(correlationId, request.Headers.GetValues(AppConstants.CORRELATION_ID));
        }

        [Fact]
        public async Task PublicSendAsync_DoesNotAddCorrelationID_WhenHeaderDoesNotExist()
        {
            // Arrange
            var httpContextAccessor = Substitute.For<IHttpContextAccessor>();
            var httpContext = new DefaultHttpContext();
            httpContextAccessor.HttpContext.Returns(httpContext);

            var handler = new TestableHeaderPropagationHandler(httpContextAccessor);
            var request = new HttpRequestMessage(HttpMethod.Get, "https://example.com");

            // Act
            var response = await handler.PublicSendAsync(request, CancellationToken.None);

            // Assert
            Assert.False(request.Headers.Contains(AppConstants.CORRELATION_ID));
        }
    }
}
