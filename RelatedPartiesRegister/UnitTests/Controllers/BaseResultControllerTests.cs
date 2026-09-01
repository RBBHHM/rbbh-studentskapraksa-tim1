using RBBH.ConnectedParties.API.Controllers.BaseController;
using RBBH.ConnectedParties.Exceptions.Validations;
using RBBH.ConnectedParties.Helpers.Constants;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace UnitTests.Controllers
{
    public class BaseResultControllerTests
    {

        // This class is needed to instantiate the abstract BaseResultController
        private class TestController : BaseResuItController
        {
        }

        #region HTTP Action Specific Error Handling

        [Fact]
        public void BadRequestFromResult_ReturnsBadRequest()
        {
            // Arrange
            var controller = new TestController();
            var result = Result<string>.ValidationError("Validation failed");
        
            var httpContext = new DefaultHttpContext();
            httpContext.Request.Headers[AppConstants.CORRELATION_ID] = "test-correlation-id";
            controller.ControllerContext.HttpContext = httpContext;

            // Act
            var actionResult = controller.BadRequestFromResult(result);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(actionResult.Result);
            var problemDetails = Assert.IsType<ProblemDetails>(badRequestResult.Value);
            Assert.Equal(StatusCodes.Status400BadRequest, problemDetails.Status);
            Assert.Equal("Validation Exception", problemDetails.Title);
            Assert.Equal("Validation failed", problemDetails.Detail);
            Assert.Equal("test-correlation-id", problemDetails.Extensions[AppConstants.CORRELATION_ID]);
        }

        [Fact]
        public void NotFoundFromResult_ReturnsNotFound()
        {
            // Arrange
            var controller = new TestController();
            var result = Result<string>.NotFoundError("Resource not found");


            var httpContext = new DefaultHttpContext();
            httpContext.Request.Headers[AppConstants.CORRELATION_ID] = "test-correlation-id";
            controller.ControllerContext.HttpContext = httpContext;

            // Act
            var actionResult = controller.NotFoundFromResult(result);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(actionResult.Result);
            var problemDetails = Assert.IsType<ProblemDetails>(notFoundResult.Value);
            Assert.Equal(StatusCodes.Status404NotFound, problemDetails.Status);
            Assert.Equal("Not Found Exception", problemDetails.Title);
            Assert.Equal("Resource not found", problemDetails.Detail);
            Assert.Equal("test-correlation-id", problemDetails.Extensions[AppConstants.CORRELATION_ID]);
        }

        [Fact]
        public void InternalServerFromResult_ReturnsInternalServerError()
        {
            // Arrange
            var controller = new TestController();
            var result = Result<string>.InternalServerError("An internal error occurred");

            var httpContext = new DefaultHttpContext();
            httpContext.Request.Headers[AppConstants.CORRELATION_ID] = "test-correlation-id";
            controller.ControllerContext.HttpContext = httpContext;

            // Act
            var actionResult = controller.InternalServerFromResult(result);

            // Assert
            var internalServerErrorResult = Assert.IsType<ObjectResult>(actionResult.Result);
            Assert.Equal(StatusCodes.Status500InternalServerError, internalServerErrorResult.StatusCode);
            var problemDetails = Assert.IsType<ProblemDetails>(internalServerErrorResult.Value);
            Assert.Equal("Internal Server Exception", problemDetails.Title);
            Assert.Equal("An internal error occurred", problemDetails.Detail);
            Assert.Equal("test-correlation-id", problemDetails.Extensions[AppConstants.CORRELATION_ID]);
        }

        #endregion

        #region Implict Error Handling

        [Fact]
        public void HttpExceptionFromResultWithValidationError_ReturnsBadRequest()
        {
            // Arrange
            var controller = new TestController();
            var result = Result<string>.ValidationError("Validation failed");

            var httpContext = new DefaultHttpContext();
            httpContext.Request.Headers[AppConstants.CORRELATION_ID] = "test-correlation-id";
            controller.ControllerContext.HttpContext = httpContext;

            // Act
            var actionResult = controller.HTTPExceptiontFromResult(result);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(actionResult.Result);
            var problemDetails = Assert.IsType<ProblemDetails>(badRequestResult.Value);
            Assert.Equal(StatusCodes.Status400BadRequest, problemDetails.Status);
            Assert.Equal("Validation Exception", problemDetails.Title);
            Assert.Equal("Validation failed", problemDetails.Detail);
            Assert.Equal("test-correlation-id", problemDetails.Extensions[AppConstants.CORRELATION_ID]);
        }

        [Fact]
        public void HttpExceptionFromResultWithNotFoundError_ReturnsNotFound()
        {
            // Arrange
            var controller = new TestController();
            var result = Result<string>.NotFoundError("Resource not found");


            var httpContext = new DefaultHttpContext();
            httpContext.Request.Headers[AppConstants.CORRELATION_ID] = "test-correlation-id";
            controller.ControllerContext.HttpContext = httpContext;

            // Act
            var actionResult = controller.HTTPExceptiontFromResult(result);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(actionResult.Result);
            var problemDetails = Assert.IsType<ProblemDetails>(notFoundResult.Value);
            Assert.Equal(StatusCodes.Status404NotFound, problemDetails.Status);
            Assert.Equal("Not Found Exception", problemDetails.Title);
            Assert.Equal("Resource not found", problemDetails.Detail);
            Assert.Equal("test-correlation-id", problemDetails.Extensions[AppConstants.CORRELATION_ID]);
        }

        [Fact]
        public void HttpExceptionFromResultWitInternalServerError_ReturnsInternalServerError()
        {
            // Arrange
            var controller = new TestController();
            var result = Result<string>.InternalServerError("An internal error occurred");

            var httpContext = new DefaultHttpContext();
            httpContext.Request.Headers[AppConstants.CORRELATION_ID] = "test-correlation-id";
            controller.ControllerContext.HttpContext = httpContext;

            // Act
            var actionResult = controller.HTTPExceptiontFromResult(result);

            // Assert
            var internalServerErrorResult = Assert.IsType<ObjectResult>(actionResult.Result);
            Assert.Equal(StatusCodes.Status500InternalServerError, internalServerErrorResult.StatusCode);
            var problemDetails = Assert.IsType<ProblemDetails>(internalServerErrorResult.Value);
            Assert.Equal("Internal Server Exception", problemDetails.Title);
            Assert.Equal("An internal error occurred", problemDetails.Detail);
            Assert.Equal("test-correlation-id", problemDetails.Extensions[AppConstants.CORRELATION_ID]);
        }

        #endregion
    }
}
