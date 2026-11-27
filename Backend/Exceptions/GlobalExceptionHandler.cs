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
                Title = statusCode >= 500 ? RBBH.ConnectedParties.Localization.BackendMessages.Text("request.unavailable.title") : RBBH.ConnectedParties.Localization.BackendMessages.Text("request.invalid.title"),
                Detail = statusCode >= 500
                    ? RBBH.ConnectedParties.Localization.BackendMessages.Text("request.unavailable.detail")
                    : RBBH.ConnectedParties.Localization.BackendMessages.Text("request.invalid.detail"),
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
                    [field] = [RBBH.ConnectedParties.Localization.BackendMessages.Text("request.invalid.detail")]
                };
            }
            // Resolve per request: the exception handler is singleton, while the catalog/database is scoped.
            var catalog = httpContext.RequestServices?.GetService<BL.Services.RuntimeLocalizationCatalog>();
            if (catalog is not null)
            {
                try
                {
                    var language = httpContext.Request.Headers.AcceptLanguage.ToString();
                    var family = statusCode >= 500 ? "unavailable" : "invalid";
                    var titleKey = $"errors.request.{family}.title";
                    var detailKey = $"errors.request.{family}.detail";
                    var rule = exception as BusinessRuleException;
                    var resourceKey = rule is null ? null : "errors." + rule.Code.Replace('_', '.');
                    if (resourceKey is not null) problem.Extensions["resourceKey"] = resourceKey;
                    var pack = await catalog.ForRequest(language, cancellationToken);
                    if (rule is not null && resourceKey is not null)
                    {
                        var translated = catalog.Text(resourceKey, pack);
                        if (translated != resourceKey) problem.Detail = string.Format(System.Globalization.CultureInfo.InvariantCulture, translated, rule.Parameters.ToArray());
                        else if (!string.IsNullOrWhiteSpace(language) && !language.StartsWith("BS", StringComparison.OrdinalIgnoreCase))
                        {
                            var genericDetail = catalog.Text(detailKey, pack);
                            if (genericDetail != detailKey) problem.Detail = genericDetail;
                        }
                    }
                    else
                    {
                        var detail = catalog.Text(detailKey, pack);
                        if (detail != detailKey) problem.Detail = detail;
                    }
                    var title = catalog.Text(titleKey, pack);
                    if (title != titleKey) problem.Title = title;
                }
                catch (Exception localizationFailure) when (localizationFailure is not OperationCanceledException)
                {
                    // A broken/missing catalog must never hide the original safe failure or leak storage details.
                    logger.LogWarning("Error localization unavailable. FailureType {FailureType}", localizationFailure.GetType().Name);
                }
            }
            await httpContext.Response.WriteAsJsonAsync(problem, cancellationToken);
            return true;
        }

    }

}
