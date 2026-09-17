using RBBH.ConnectedParties.Helpers.Constants;
using RBBH.ConnectedParties.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using NSubstitute;
using System.Text.Json;

namespace UnitTests.Middleware
{

    public class GlobalExceptionHandlerTests
    {
        [Fact]
        public async Task TryHandleAsync_LogsErrorAndSetsStatusCode_WhenExceptionOccurs()
        {
            // Arrange
            var logger = Substitute.For<ILogger<GlobalExceptionHandler>>();
            var handler = new GlobalExceptionHandler(logger);

            var httpContext = new DefaultHttpContext();
            httpContext.Request.Method = "GET";
            httpContext.Request.Path = "/test";
            httpContext.Request.Headers[AppConstants.CORRELATION_ID] = "test-correlation-id";

            var exception = new ApplicationException("Test exception");

            // Act
            var result = await handler.TryHandleAsync(httpContext, exception, CancellationToken.None);

            // Assert
            Assert.True(result); // Handler je zapisao siguran ProblemDetails odgovor.
            Assert.Equal(StatusCodes.Status400BadRequest, httpContext.Response.StatusCode); // Check the status code
        }

        [Fact]
        public async Task TryHandleAsync_HandlesNonApplicationException()
        {
            // Arrange
            var logger = Substitute.For<ILogger<GlobalExceptionHandler>>();
            var handler = new GlobalExceptionHandler(logger);

            var httpContext = new DefaultHttpContext();
            httpContext.Request.Method = "GET";
            httpContext.Request.Path = "/test";
            httpContext.Request.Headers[AppConstants.CORRELATION_ID] = "test-correlation-id";

            var exception = new Exception("General exception"); // Non-ApplicationException

            // Act
            var result = await handler.TryHandleAsync(httpContext, exception, CancellationToken.None);

            // Assert
            Assert.True(result); // Handler je zapisao siguran ProblemDetails odgovor.
            Assert.Equal(StatusCodes.Status500InternalServerError, httpContext.Response.StatusCode); // Check the status code
        }

        [Fact]
        public async Task TryHandleAsync_ReturnsFriendlyBadRequest_ForFieldValidationException()
        {
            var logger = Substitute.For<ILogger<GlobalExceptionHandler>>();
            var handler = new GlobalExceptionHandler(logger);
            var httpContext = new DefaultHttpContext();
            httpContext.Request.Method = "PUT";
            httpContext.Request.Path = "/api/verification/legal-person/record-id";
            httpContext.Response.Body = new MemoryStream();

            await handler.TryHandleAsync(
                httpContext,
                new ValidationException("verifiedBy", "Ne možete verificirati zapis koji ste sami unijeli."),
                CancellationToken.None);

            Assert.Equal(StatusCodes.Status400BadRequest, httpContext.Response.StatusCode);
            httpContext.Response.Body.Position = 0;
            using var document = await JsonDocument.ParseAsync(httpContext.Response.Body);
            Assert.Equal(
                "Ne možete verificirati zapis koji ste sami unijeli.",
                document.RootElement.GetProperty("detail").GetString());
            Assert.Equal(
                "Ne možete verificirati zapis koji ste sami unijeli.",
                document.RootElement.GetProperty("errors").GetProperty("verifiedBy")[0].GetString());
        }
    }
}
