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
            if (exception is ValidationException { Field: { Length: > 0 } field })
            {
                // Business messages can interpolate identifiers. Publish the field,
                // not raw exception text or submitted personal/business data.
                problem.Extensions["errors"] = new Dictionary<string, string[]>
                {
                    [field] = ["Provjerite unesene podatke i pokušajte ponovo."]
                };
            }
            await httpContext.Response.WriteAsJsonAsync(problem, cancellationToken);
            return true;
        }

    }

}
