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

            if (!await database.Database.CanConnectAsync(cancellationToken))
                return HealthCheckResult.Unhealthy("Konfigurisana baza nije dostupna.");
            // Inspection only: readiness never creates tables or applies DDL.
            if ((await database.Database.GetPendingMigrationsAsync(cancellationToken)).Any())
                return HealthCheckResult.Unhealthy("Baza zahtijeva odobrenu migraciju prije pokretanja saobraćaja.");
            return HealthCheckResult.Healthy("Konfigurisana baza i migracije su dostupne.");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch (Exception exception)
        {
            return HealthCheckResult.Unhealthy("Provjera baze nije uspjela.", data: new Dictionary<string, object> { ["failureType"] = exception.GetType().Name });
        }
    }
}
