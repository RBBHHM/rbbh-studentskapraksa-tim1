using RBBH.ConnectedParties.Helpers.Utils;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace RBBH.ConnectedParties.IoC.Extensions.Health
{
    public static class HealthCheckServices
    {
        public static IServiceCollection AddSelftHealthCheck(this IServiceCollection services)
        {
            services.AddHealthChecks()
                .AddCheck("self", () => HealthCheckResult.Healthy("Service is alive"));

            return services;
        }

        public static IServiceCollection AddDependencyHealthCheck(this IServiceCollection services, IConfiguration configuration)
        {
            var connectionString = ConnectionHelper.BuildConnection(configuration);

            services.AddHealthChecks().AddAsyncCheck(
                "database",
                async cancellationToken =>
                {
                    try
                    {
                        await using var connection = new SqlConnection(connectionString);
                        await connection.OpenAsync(cancellationToken);
                        return HealthCheckResult.Healthy("SQL Server is reachable.");
                    }
                    catch (Exception exception)
                    {
                        return HealthCheckResult.Unhealthy("SQL Server is unavailable.", exception);
                    }
                },
                tags: new[] { "ready" });

            return services;
        }

        public static IServiceCollection TryAddDependencyHealthCheck(this IServiceCollection services, IConfiguration configuration)
        {
            try
            {
                return services.AddDependencyHealthCheck(configuration);
            }
            catch (InvalidOperationException)
            {
                return services;
            }
        }
    }
}
