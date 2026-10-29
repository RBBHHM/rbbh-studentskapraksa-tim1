using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace RBI.Hosting;

/// <summary>
/// Registers and applies the common RBI hosting contract.
/// </summary>
public static class RbiHostingExtensions
{
    private const string LivePath = "/health/live";
    private const string ReadyPath = "/health/ready";

    /// <summary>
    /// Registers framework health checks and safe forwarded-header processing.
    /// </summary>
    /// <param name="services">The application services.</param>
    /// <param name="configureForwardedHeaders">
    /// An optional deployment-owned trust configuration. Framework loopback trust remains the safe default.
    /// </param>
    /// <returns>The same service collection.</returns>
    public static IServiceCollection AddRbiHosting(
        this IServiceCollection services,
        Action<ForwardedHeadersOptions>? configureForwardedHeaders = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddHealthChecks();
        services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
        });

        if (configureForwardedHeaders is not null)
            services.Configure(configureForwardedHeaders);

        return services;
    }

    /// <summary>
    /// Applies trusted forwarded headers and maps the anonymous liveness and readiness endpoints.
    /// Call immediately after building the application and before HTTPS redirection.
    /// </summary>
    /// <param name="app">The web application.</param>
    /// <returns>The same web application.</returns>
    public static WebApplication UseRbiHosting(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.UseForwardedHeaders();
        app.MapHealthChecks(LivePath, CreateHealthOptions(static _ => false)).AllowAnonymous();
        app.MapHealthChecks(
                ReadyPath,
                CreateHealthOptions(static registration => registration.Tags.Contains(RbiHealthCheckTags.Ready)))
            .AllowAnonymous();

        return app;
    }

    private static HealthCheckOptions CreateHealthOptions(Func<HealthCheckRegistration, bool> predicate) => new()
    {
        Predicate = predicate,
        ResponseWriter = WriteMinimalResponseAsync,
    };

    private static Task WriteMinimalResponseAsync(HttpContext context, HealthReport report)
    {
        context.Response.ContentType = "text/plain; charset=utf-8";
        return context.Response.WriteAsync(report.Status.ToString());
    }
}
