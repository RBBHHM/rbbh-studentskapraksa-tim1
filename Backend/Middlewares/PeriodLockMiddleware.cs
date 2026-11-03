using System.Text.Json;
using RBBH.ConnectedParties.BL.ServiceInterfaces;
using RBBH.ConnectedParties.Helpers;

namespace RBBH.ConnectedParties.Middlewares;

public class PeriodLockMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<PeriodLockMiddleware> _logger;

    public PeriodLockMiddleware(RequestDelegate next, ILogger<PeriodLockMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, IPeriodLockRepository periodLockRepository)
    {
        if (ShouldSkipPeriodLockCheck(context))
        {
            await _next(context);
            return;
        }

        var isLocked = await periodLockRepository.IsCurrentPeriodLockedAsync();

        if (isLocked)
        {
            _logger.LogWarning(
                "Global period lock blocked request. Method: {Method}, Path: {Path}",
                context.Request.Method, context.Request.Path);

            context.Response.StatusCode = StatusCodes.Status423Locked;
            context.Response.ContentType = "application/json";

            const string resourceKey = "errors.period.locked";
            var message = BackendMessages.Message(resourceKey);
            try
            {
                var catalog = context.RequestServices?.GetService<BL.Services.RuntimeLocalizationCatalog>();
                if (catalog is not null)
                {
                    var translated = await catalog.Text(resourceKey, context.Request.Headers.AcceptLanguage.ToString(), context.RequestAborted);
                    if (translated != resourceKey)
                    {
                        if (System.Text.CompositeFormat.Parse(translated).MinimumArgumentCount != 0) throw new FormatException();
                        message = translated;
                    }
                }
            }
            catch (Exception error) when (error is not OperationCanceledException)
            { _logger.LogWarning("Period lock localization unavailable. FailureType {FailureType}", error.GetType().Name); }
            var response = new
            {
                errors = new[]
                {
                    new
                    {
                        field = (string?)null,
                        message
                    }
                }
            };

            await context.Response.WriteAsync(JsonSerializer.Serialize(response));
            return;
        }

        await _next(context);
    }

    private static bool ShouldSkipPeriodLockCheck(HttpContext context)
    {
        var method = context.Request.Method;
        var path = context.Request.Path.Value ?? string.Empty;

        if (HttpMethods.IsGet(method))
            return true;

        if (!HttpMethods.IsPost(method) &&
            !HttpMethods.IsPut(method) &&
            !HttpMethods.IsDelete(method))
            return true;

        if (path.StartsWith("/api/period-lock/", StringComparison.OrdinalIgnoreCase))
            return true;

        if (path.StartsWith("/api/auth/", StringComparison.OrdinalIgnoreCase))
            return true;

        // Mutations that change period/reportable business data. Capital edits and
        // entity verification change reportable state, so they are locked too.
        // Report generation, user administration, code lists and period administration
        // are deliberately not locked (see the route matrix in PeriodLockCoverageTests).
        var protectedBusinessPath =
            path.StartsWith("/api/related-persons", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("/api/legal-entities", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("/api/limiti", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("/api/capital", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("/api/verification", StringComparison.OrdinalIgnoreCase);

        return !protectedBusinessPath;
    }
}
