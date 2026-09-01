using RBBH.ConnectedParties.Exceptions.Validations;
using RBBH.ConnectedParties.Helpers.Constants;
using Microsoft.AspNetCore.Mvc;

namespace RBBH.ConnectedParties.API.Controllers.BaseController
{
    /// <summary>
    /// Custom Controller Base that accepts Result payload and returns ProblemDetails exception
    /// </summary>
    public abstract class BaseResuItController : ControllerBase
    {
        #region ActionResult<T>

        /// <summary>
        /// Handles all responses implictly
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="result"></param>
        /// <returns>Appropriate HTTP Status Code wrapped into ProblemDetails.</returns>
        public ActionResult<T> HandleResult<T>(Result<T> result)
        {
            if (result.IsSuccessful)
            {
                return Ok(result.Value);
            }

            return HTTPExceptiontFromResult(result);
        }

        /// <summary>
        /// Generic HTTP Response that figures out what response code to use
        /// When you do not want to be explictily check each validation in the controller
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="result"></param>
        /// <returns>Appropriate HTTP Status Code wrapped into ProblemDetails.</returns>
        public ActionResult<T> HTTPExceptiontFromResult<T>(Result<T> result)
        {
            return result.IsValidationError ?
                BadRequestFromResult(result) :
                    result.IsNotFoundError ?
                NotFoundFromResult(result) :
                    InternalServerFromResult(result);
        }

        /// <summary>
        /// Accepts Result of T and wraps it into a ProblemDetails with Bad Request Exception
        /// </summary>
        /// <typeparam name="T">Any generic type.</typeparam>
        /// <param name="result">Result instance.</param>
        /// <returns>Action Result ProblemDetails with status 400.</returns>
        public ActionResult<T> BadRequestFromResult<T>(Result<T> result)
        {
            var problemDetails = ToProblemDetails(result, StatusCodes.Status400BadRequest, Request);
            return BadRequest(problemDetails);
        }

        /// <summary>
        /// Accepts Result of T and wraps it into a ProblemDetails with Not Found Exception
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="result"></param>
        /// <returns>Action Result ProblemDetails with status 404.</returns>
        public ActionResult<T> NotFoundFromResult<T>(Result<T> result)
        {
            var problemDetails = ToProblemDetails(result, StatusCodes.Status404NotFound, Request);
            return NotFound(problemDetails);
        }

        /// <summary>
        /// Accepts Result of T and wraps it into a ProblemDetails with Internal Server Exception
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="result"></param>
        /// <returns>Action Result ProblemDetails with status 500.</returns>
        public ActionResult<T> InternalServerFromResult<T>(Result<T> result)
        {
            var problemDetails = ToProblemDetails(result, StatusCodes.Status500InternalServerError, Request);
            return StatusCode(500, problemDetails);
        }

        #endregion

        /// <summary>
        /// Creates a new instance of ProblemDetails class
        /// </summary>
        /// <typeparam name="T">Generic type.</typeparam>
        /// <param name="result">Result of T.</param>
        /// <param name="statusCode">HTTP Response Status Code.</param>
        /// <param name="request">HTTP Request object.</param>
        /// <returns>Mapped ProblemDetails instance.</returns>
        private static ProblemDetails ToProblemDetails<T>(Result<T> result, int statusCode, HttpRequest request)
        {
            return new ProblemDetails
            {
                Title = GetExceptionTitle(result.ExceptionType),
                Detail = result.ExceptionMessage,
                Status = statusCode,
                Instance = $"{request.Method} {request.Path}",
                Extensions = new Dictionary<string, object?>
                {
                    [AppConstants.CORRELATION_ID] = request.Headers[AppConstants.CORRELATION_ID].ToString(),
                    ["timestamp"] = DateTime.UtcNow
                }
            };
        }

        /// <summary>
        /// Returns Exceotion name based on the ResultException status
        /// </summary>
        /// <param name="ex">ResultException.</param>
        /// <returns>Exception name.</returns>
        private static string GetExceptionTitle(ResultException ex)
        {
            return ex switch
            {
                ResultException.ValidationException => "Validation Exception",
                ResultException.NotFoundException => "Not Found Exception",
                ResultException.InternalException => "Internal Server Exception",
                _ => "Internal Server Exception",
            };
        }
    }
}
