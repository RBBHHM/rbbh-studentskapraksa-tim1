using System.Diagnostics;
using Microsoft.AspNetCore.Routing;

namespace RBBH.ConnectedParties.IoC.Middleware;

/// <summary>Records API outcomes without identifiers, claims, tokens or query strings.</summary>
public sealed class RequestOutcomeLoggingMiddleware(RequestDelegate next, ILogger<RequestOutcomeLoggingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        if (!context.Request.Path.StartsWithSegments("/api"))
        {
            await next(context);
            return;
        }

        var started = Stopwatch.GetTimestamp();
        await next(context);

        // Route templates retain the operation name without concrete path identifiers.
        var route = (context.GetEndpoint() as RouteEndpoint)?.RoutePattern.RawText ?? "(unmatched)";
        var status = context.Response.StatusCode;
        logger.Log(status >= 400 ? LogLevel.Warning : LogLevel.Information,
            "API request completed. Method {RequestMethod}. Route {Route}. Status {StatusCode}. ElapsedMs {ElapsedMs}. TraceId {TraceId}",
            context.Request.Method, route, status, Stopwatch.GetElapsedTime(started).TotalMilliseconds,
            context.TraceIdentifier);
    }
}
