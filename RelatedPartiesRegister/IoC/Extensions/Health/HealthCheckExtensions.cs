using RBBH.ConnectedParties.Helpers.Constants;

namespace RBBH.ConnectedParties.IoC.Extensions.Health
{
    public static class HealthCheckExtensions
    {
        public static IServiceCollection AddHealthCheckExtension(this IServiceCollection services, IConfiguration configuration)
        {
            ArgumentNullException.ThrowIfNull(services);
            ArgumentNullException.ThrowIfNull(configuration);

            services.AddSelftHealthCheck();

            services.AddHealthChecks()
                .AddCheck<DatabaseHealthCheck>("database", tags: ["ready"]);

            return services;
        }
    }
}
