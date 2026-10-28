using RBBH.ConnectedParties.DL.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace RBBH.ConnectedParties.IoC.Extensions.Health;

public sealed class DatabaseHealthCheck(ConnectedPartiesDbContext database) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (!database.Database.IsRelational())
                return HealthCheckResult.Healthy("Lokalna in-memory baza je dostupna.");

            return await database.Database.CanConnectAsync(cancellationToken)
                ? HealthCheckResult.Healthy("Konfigurisana baza je dostupna.")
                : HealthCheckResult.Unhealthy("Konfigurisana baza nije dostupna.");
        }
        catch (Exception exception)
        {
            return HealthCheckResult.Unhealthy("Provjera baze nije uspjela.", exception);
        }
    }
}
