using Microsoft.Data.SqlClient;

namespace RBBH.ConnectedParties.Helpers.Utils
{
    public static class ConnectionHelper
    {
        public static string BuildConnection(
            IConfiguration configuration
        )
        {
            ArgumentNullException.ThrowIfNull(configuration);

            var environment = configuration["ASPNETCORE_ENVIRONMENT"] ?? "Production";
            var integratedSecurity = configuration.GetValue<bool?>("Database:IntegratedSecurity") ?? false;
            var builder = new SqlConnectionStringBuilder
            {
                DataSource = GetRequiredValue(configuration, "Database:ServerName"),
                InitialCatalog = GetRequiredValue(configuration, "Database:Name"),
                IntegratedSecurity = integratedSecurity,
                MultipleActiveResultSets = true,
                Encrypt = true,
                TrustServerCertificate = environment.Equals("Development", StringComparison.OrdinalIgnoreCase),
                ConnectTimeout = 15,
                CommandTimeout = 30,
                ConnectRetryCount = 3,
                ConnectRetryInterval = 5,
                Pooling = true,
                MinPoolSize = 5,
                MaxPoolSize = 100
            };

            if (!integratedSecurity)
            {
                builder.UserID = GetRequiredValue(configuration, "Database:User");
                builder.Password = GetRequiredValue(configuration, "Database:Password");
            }

            return builder.ConnectionString;
        }

        public static bool IsConfigured(IConfiguration configuration)
        {
            ArgumentNullException.ThrowIfNull(configuration);
            return !string.IsNullOrWhiteSpace(configuration["Database:ServerName"])
                || !string.IsNullOrWhiteSpace(configuration["Database:Name"]);
        }

        private static string GetRequiredValue(IConfiguration configuration, string key)
        {
            var value = configuration[key];
            return string.IsNullOrWhiteSpace(value)
                ? throw new InvalidOperationException($"Required database setting '{key}' is missing or empty.")
                : value;
        }
    }
}
