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

            // Database/authentication exception messages can contain submitted data.
            // Keep operational diagnostics useful without serializing the exception.
            logger.LogError(
                "Request failed. Method {RequestMethod}. Route {Route}. FailureType {FailureType}. Status {StatusCode}. TraceId {TraceId}",
                httpContext.Request.Method,
                ((httpContext.GetEndpoint() ?? httpContext.Features.Get<IExceptionHandlerFeature>()?.Endpoint)
                    as Microsoft.AspNetCore.Routing.RouteEndpoint)?.RoutePattern.RawText ?? "(unmatched)",
                exception.GetType().Name,
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
            if (exception is BusinessRuleException business)
            {
                problem.Extensions["code"] = business.Code;
                problem.Detail = business.Message;
            }
            else if (exception is Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException)
                problem.Extensions["code"] = "request.concurrent_change";
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
