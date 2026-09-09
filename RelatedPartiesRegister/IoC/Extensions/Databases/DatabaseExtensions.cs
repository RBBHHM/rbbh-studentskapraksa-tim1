using RBBH.ConnectedParties.DL.Persistence;
using RBBH.ConnectedParties.Helpers.Utils;
using Microsoft.EntityFrameworkCore;

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
            var connectionString = configured
                ? ConnectionHelper.BuildConnection(configuration)
                : null;

            if (!configured && environment.IsDevelopment())
                services.AddSingleton(new DatabaseStartupWarning(
                    "SQL Server nije konfigurisan. Aplikacija koristi privremenu seedovanu InMemory bazu; " +
                    "podaci će nestati nakon gašenja API-ja."));
                
            services.AddDbContext<ConnectedPartiesDbContext>(options =>
            {
                if (!configured && environment.IsDevelopment())
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
    }

    public sealed record DatabaseStartupWarning(string Message);
}
