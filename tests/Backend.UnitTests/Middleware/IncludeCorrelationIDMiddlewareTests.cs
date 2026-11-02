using RBBH.ConnectedParties.Helpers.Constants;
using RBBH.ConnectedParties.IoC.Middleware;
using Microsoft.AspNetCore.Http;
using NSubstitute;

namespace UnitTests.Middleware
{
    public class IncludeCorrelationIDMiddlewareTests
    {
        [Theory]
        [InlineData("")]
        [InlineData("private@example.test")]
        [InlineData("line\r\nbreak")]
        [InlineData("first,second")]
        [InlineData("å")]
        [InlineData("01234567890123456789012345678901234567890123456789012345678901234")]
        public async Task InvalidCorrelationIsReplacedBeforeDownstreamUse(string value)
        {
            var context = new DefaultHttpContext();
            context.Request.Headers[AppConstants.CORRELATION_ID] = value;
            var middleware = new IncludeCorrelationIDMiddleware(nextContext =>
            {
                Assert.True(Guid.TryParse(context.Request.Headers[AppConstants.CORRELATION_ID], out _));
                return Task.CompletedTask;
            });
            await middleware.InvokeAsync(context);
            Assert.Equal(context.Request.Headers[AppConstants.CORRELATION_ID], context.Response.Headers[AppConstants.CORRELATION_ID]);
        }

        [Fact]
        public async Task MultipleCorrelationValuesAreReplaced()
        {
            var context = new DefaultHttpContext();
            context.Request.Headers.Append(AppConstants.CORRELATION_ID, new[] { "first", "second" });
            await new IncludeCorrelationIDMiddleware(_ => Task.CompletedTask).InvokeAsync(context);
            Assert.True(Guid.TryParse(context.Request.Headers[AppConstants.CORRELATION_ID], out _));
        }

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
