using RBBH.ConnectedParties.DL.Persistence;
using RBBH.ConnectedParties.Helpers.Utils;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;

namespace RBBH.ConnectedParties.IoC.Extensions.Databases
{
    public static class DatabaseExtensions
    {
        public static IServiceCollection AddDatabaseExtension(
        this IServiceCollection services,
        IConfiguration configuration,
        IWebHostEnvironment environment
        )
        {
            ArgumentNullException.ThrowIfNull(services);
            ArgumentNullException.ThrowIfNull(configuration);

            var configured = ConnectionHelper.IsConfigured(configuration);
            string? connectionString = null;
            string? fallbackReason = null;

            if (configured)
            {
                connectionString = ConnectionHelper.BuildConnection(configuration);

                if (environment.IsDevelopment() && !CanConnect(connectionString, out fallbackReason))
                {
                    services.AddSingleton(new DatabaseStartupWarning(
                        "Konfigurisani SQL Server trenutno nije dostupan. " +
                        "Aplikacija koristi privremenu seedovanu InMemory bazu; podaci će nestati nakon gašenja API-ja. " +
                        $"Razlog: {fallbackReason}"));
                }
            }

            var useInMemory = !configured || fallbackReason is not null;
                
            services.AddDbContext<ConnectedPartiesDbContext>(options =>
            {
                if (useInMemory && environment.IsDevelopment())
                {
                    options.UseInMemoryDatabase("connected-parties-local");
                    return;
                }

                if (!configured)
                    throw new InvalidOperationException(
                        "SQL Server nije konfigurisan. Postavite Database__ServerName i Database__Name.");

                options.UseSqlServer(
                    connectionString!,
                    sql => sql.EnableRetryOnFailure(3, TimeSpan.FromSeconds(5), null));
            });

            return services;
        }

        private static bool CanConnect(string connectionString, out string? failureReason)
        {
            var probeBuilder = new SqlConnectionStringBuilder(connectionString)
            {
                ConnectTimeout = 3,
                ConnectRetryCount = 0,
                Pooling = false
            };

            var probe = Task.Run(() =>
            {
                try
                {
                    using var connection = new SqlConnection(probeBuilder.ConnectionString);
                    connection.Open();
                    return (Connected: true, Reason: (string?)null);
                }
                catch (SqlException exception)
                {
                    return (Connected: false, Reason: $"SQL veza nije uspostavljena (kod {exception.Number}).");
                }
                catch (TimeoutException)
                {
                    return (Connected: false, Reason: "Isteklo je vrijeme za povezivanje sa SQL Serverom.");
                }
            });

            // Named SQL instances can remain in network discovery longer than
            // SqlConnection.ConnectTimeout. Never block local onboarding on it.
            if (!probe.Wait(TimeSpan.FromSeconds(5)))
            {
                failureReason = "Isteklo je vrijeme za povezivanje sa SQL Serverom.";
                return false;
            }

            var result = probe.GetAwaiter().GetResult();
            failureReason = result.Reason;
            return result.Connected;
        }
    }

    public sealed record DatabaseStartupWarning(string Message);
}
