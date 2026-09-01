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

            logger.LogError(
                 exception,
                 "Unhandled exception for {RequestMethod} {RequestPath}. Status {StatusCode}. TraceId {TraceId}",
                 httpContext.Request.Method,
                 httpContext.Request.Path.Value,
                 statusCode,
                 httpContext.TraceIdentifier);

            var problem = new ProblemDetails
            {
                Status = statusCode,
                Title = statusCode >= 500 ? "Zahtjev trenutno nije moguće završiti." : "Zahtjev nije ispravan.",
                Detail = statusCode >= 500
                    ? "Pokušajte ponovo. Ako se problem ponovi, podršci pošaljite navedeni ID."
                    : "Provjerite unesene podatke i pokušajte ponovo.",
                Instance = $"{httpContext.Request.Method} {httpContext.Request.Path}"
            };
            problem.Extensions["traceId"] = httpContext.TraceIdentifier;
            await httpContext.Response.WriteAsJsonAsync(problem, cancellationToken);
            return true;
        }

    }

}
