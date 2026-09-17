using RBBH.ConnectedParties.Exceptions.Helpers;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace RBBH.ConnectedParties.Exceptions
{
    public sealed class GlobalExceptionHandler(
        ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
    {
        public async ValueTask<bool> TryHandleAsync(
            HttpContext httpContext,
            Exception exception,
            CancellationToken cancellationToken)
        {
            int statusCode = ExceptionToStatusCodeHelper.GetStatus(exception);
            httpContext.Response.StatusCode = statusCode;

            if (statusCode >= 500)
            {
                logger.LogError(
                    exception,
                    "Unhandled exception for {RequestMethod} {RequestPath}. Status {StatusCode}. TraceId {TraceId}",
                    httpContext.Request.Method,
                    httpContext.Request.Path.Value,
                    statusCode,
                    httpContext.TraceIdentifier);
            }
            else
            {
                logger.LogWarning(
                    "Request validation failed for {RequestMethod} {RequestPath}. Status {StatusCode}. TraceId {TraceId}",
                    httpContext.Request.Method,
                    httpContext.Request.Path.Value,
                    statusCode,
                    httpContext.TraceIdentifier);
            }

            string detail = exception switch
            {
                ValidationException validation => validation.ErrorMessage,
                Custom.ValidationException validation => validation.Message,
                ApplicationException application => application.Message,
                _ => "Pokušajte ponovo. Ako se problem ponovi, podršci pošaljite navedeni ID."
            };

            var problem = new ProblemDetails
            {
                Status = statusCode,
                Title = statusCode >= 500 ? "Zahtjev trenutno nije moguće završiti." : "Zahtjev nije ispravan.",
                Detail = detail,
                Instance = $"{httpContext.Request.Method} {httpContext.Request.Path}"
            };
            if (exception is ValidationException fieldValidation && fieldValidation.Field is not null)
            {
                problem.Extensions["errors"] = new Dictionary<string, string[]>
                {
                    [fieldValidation.Field] = [fieldValidation.ErrorMessage]
                };
            }
            else if (exception is Custom.ValidationException customValidation && customValidation.Errors.Count > 0)
            {
                problem.Extensions["errors"] = customValidation.Errors;
            }
            problem.Extensions["traceId"] = httpContext.TraceIdentifier;
            await httpContext.Response.WriteAsJsonAsync(problem, cancellationToken);
            return true;
        }

    }

}
