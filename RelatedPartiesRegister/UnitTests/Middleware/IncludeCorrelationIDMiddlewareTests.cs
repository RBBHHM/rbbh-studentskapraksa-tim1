using RBBH.ConnectedParties.Helpers.Constants;
using RBBH.ConnectedParties.IoC.Middleware;
using Microsoft.AspNetCore.Http;
using NSubstitute;

namespace UnitTests.Middleware
{
    public class IncludeCorrelationIDMiddlewareTests
    {
        [Fact]
        public async Task InvokeAsync_AddsCorrelationID_WhenHeaderDoesNotExist()
        {
            // Arrange
            var context = new DefaultHttpContext();
            context.Request.Headers.Clear(); // Ensure headers are empty

            var nextMiddleware = Substitute.For<RequestDelegate>();
            var middleware = new IncludeCorrelationIDMiddleware(nextMiddleware);

            // Act
            await middleware.InvokeAsync(context);

            // Assert
            Assert.True(context.Request.Headers.ContainsKey(AppConstants.CORRELATION_ID));
            await nextMiddleware.Received(1)(context);
        }

        [Fact]
        public async Task InvokeAsync_DoesNotAddCorrelationID_WhenHeaderExists()
        {
            // Arrange
            var correlationID = "1234-5678-9012-3456";
            var context = new DefaultHttpContext();
            context.Request.Headers[AppConstants.CORRELATION_ID] = correlationID;

            var nextMiddleware = Substitute.For<RequestDelegate>();
            var middleware = new IncludeCorrelationIDMiddleware(nextMiddleware);

            // Act
            await middleware.InvokeAsync(context);

            // Assert
            Assert.Equal(correlationID, context.Request.Headers[AppConstants.CORRELATION_ID]);
            await nextMiddleware.Received(1)(context);
        }
    }
}