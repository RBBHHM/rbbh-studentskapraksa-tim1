using RBI.Authorization;

namespace RBBH.RelatedPartiesRegister.Web.Api;

/// <summary>
/// Shared navigation projection for the dashboard and sidebar. Resolve effective
/// admin status from the DB-owned profile; individual module permissions remain
/// evaluated through the existing authorization service. No cross-request
/// caching: role changes and disabled identities must not retain stale menus.
/// </summary>
public sealed record ApplicationNavigationAccess(
    bool Administrator,
    bool Reports,
    bool Physical,
    bool Legal,
    bool Limits,
    bool Capital)
{
    public static async Task<ApplicationNavigationAccess> ReadAsync(
        IProfileApi profile, IRbiPermissionService permissions,
        CancellationToken cancellationToken = default)
    {
        var profileTask = profile.GetAsync(cancellationToken);
        var reportsTask = permissions.HasPermissionAsync("reports.read", cancellationToken).AsTask();
        var physicalTask = permissions.HasPermissionAsync("fl.read", cancellationToken).AsTask();
        var legalTask = permissions.HasPermissionAsync("pl.read", cancellationToken).AsTask();
        var limitsTask = permissions.HasPermissionAsync("limiti.read", cancellationToken).AsTask();
        var capitalTask = permissions.HasPermissionAsync("kapital.read", cancellationToken).AsTask();

        // Independent authorization checks can run together instead of a
        // serial chain during first render. Await every task before rendering.
        await Task.WhenAll(profileTask, reportsTask, physicalTask, legalTask, limitsTask, capitalTask);
        return new(
            ApplicationAccess.IsAdministrator(await profileTask),
            await reportsTask,
            await physicalTask,
            await legalTask,
            await limitsTask,
            await capitalTask);
    }
}
