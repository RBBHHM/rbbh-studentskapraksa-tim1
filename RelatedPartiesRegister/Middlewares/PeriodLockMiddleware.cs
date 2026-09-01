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

        var username = context.User.FindFirst("preferred_username")?.Value
            ?? context.User.Identity?.Name;
        var department = DepartmentHelper.ExtractDepartment(username);

        var isLocked = await periodLockRepository.IsCurrentPeriodLockedAsync(department);

        if (isLocked)
        {
            _logger.LogWarning(
                "Period lock blocked request. Method: {Method}, Path: {Path}, Department: {Department}",
                context.Request.Method, context.Request.Path, department ?? "global");

            context.Response.StatusCode = StatusCodes.Status423Locked;
            context.Response.ContentType = "application/json";

            var response = new
            {
                errors = new[]
                {
                    new
                    {
                        field = (string?)null,
                        message = "Unos podataka je zaključan za ovaj period."
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

        var protectedBusinessPath =
            path.StartsWith("/api/related-persons", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("/api/legal-entities", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("/api/limiti", StringComparison.OrdinalIgnoreCase);

        return !protectedBusinessPath;
    }
}
