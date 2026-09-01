namespace RBBH.ConnectedParties.IoC.Extensions.CORS
{
    public static class CORSExtensions
    {
        public static IServiceCollection AddCORSExtension(
            this IServiceCollection services,
            IConfiguration configuration,
            IWebHostEnvironment environment)
        {
            ArgumentNullException.ThrowIfNull(services);
            ArgumentNullException.ThrowIfNull(configuration);
            ArgumentNullException.ThrowIfNull(environment);

            var configuredOrigins = configuration
                .GetSection("Cors:AllowedOrigins")
                .Get<string[]>() ?? [];

            var origins = configuredOrigins
                .Where(origin => Uri.TryCreate(origin, UriKind.Absolute, out var uri)
                    && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
                .Select(origin => origin.TrimEnd('/'))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (environment.IsDevelopment())
            {
                origins.AddRange([
                    "http://127.0.0.1:8080", "http://localhost:8080",
                    "http://127.0.0.1:8081", "http://localhost:8081",
                    "http://127.0.0.1:8082", "http://localhost:8082"
                ]);
                origins = origins.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            }

            services.AddCors(options =>
            {
                options.AddDefaultPolicy(policy =>
                {
                    // Without an explicit production allow-list the browser receives no
                    // cross-origin permission. A wildcard is intentionally never used.
                    if (origins.Count > 0)
                    {
                        policy.WithOrigins(origins.ToArray())
                            .AllowAnyMethod()
                            .AllowAnyHeader()
                            .AllowCredentials();
                    }
                });
            });

            return services;
        }
    }
}
