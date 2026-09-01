using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Text.Json;

namespace RBBH.ConnectedParties.IoC.Extensions.Health
{
    public static class HealthCheckEndpoints
    {
        public static IEndpointRouteBuilder MapCustomHealthChecks(this IEndpointRouteBuilder builder)
        {
            // Liveness (checks if service is running)
            builder.MapHealthChecks("/health/live", new HealthCheckOptions
            {
                Predicate = check => check.Name == "self", // only "self"
                ResponseWriter = WriteHealthResponse,
                ResultStatusCodes = _HealthStatuses
            });

            // Readiness (checks if dependencies are available) 
            builder.MapHealthChecks("/health/ready", new HealthCheckOptions
            {
                Predicate = check => check.Tags.Contains("ready"), // only tagged checks
                ResponseWriter = WriteHealthResponse,
                ResultStatusCodes = _HealthStatuses
            });

            return builder;
        }

        #region private fields

        private static readonly IDictionary<HealthStatus, int> _HealthStatuses = new Dictionary<HealthStatus, int>
        {
            { HealthStatus.Healthy, StatusCodes.Status200OK },
            { HealthStatus.Degraded, StatusCodes.Status200OK },
            { HealthStatus.Unhealthy, StatusCodes.Status503ServiceUnavailable }
        };

        private static Task WriteHealthResponse(HttpContext context, HealthReport report)
        {
            context.Response.ContentType = "application/json";

            var result = JsonSerializer.Serialize(new
            {
                status = report.Status.ToString(),
                checks = report.Entries.Select(e => new
                {
                    name = e.Key,
                    status = e.Value.Status.ToString(),
                    description = e.Value.Description
                }),
                duration = report.TotalDuration
            });

            return context.Response.WriteAsync(result);
        }

        #endregion

    }


}
